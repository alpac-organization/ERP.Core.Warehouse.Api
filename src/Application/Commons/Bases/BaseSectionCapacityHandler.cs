using MediatR;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Database.Application.Commons.Interfaces.Services.WarehouseCapacities;
using ERP.Core.Database.Domain.Entities.Warehouse;

namespace ERP.Core.Warehouse.Api.Application.Commons.Bases;

public abstract class BaseSectionCapacityHandler<TRequest>(
    IUnitOfWork unitOfWork,
    IErrorManager errorManager,
    IMapper mapper,
    ISectionCapacityCalculator sectionCapacityCalculator)
    : BaseValidatorHandler<TRequest, bool>(unitOfWork, errorManager)
    where TRequest : IRequest<bool>
{
    protected readonly IMapper _mapper = mapper;
    protected readonly ISectionCapacityCalculator _sectionCapacityCalculator = sectionCapacityCalculator;

    protected async Task<(bool IsValid, bool ErrorResponse)> EnsureAdministratorAccessAsync(
        Guid userId,
        Guid companyId,
        string moduleCode,
        CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(userId, companyId, moduleCode, cancellationToken);

        if (!access.IsSuccess)
            return (false, access.ErrorResponse!);

        if (access.Role?.RoleType != RoleType.Administrator)
            return (false, _errorManager.ThrowBadRequest<bool>(
                "No tienes permiso para realizar esta acción", "ERP:01"));

        return (true, default);
    }

    protected async Task<(bool IsValid, Sections? Section, bool ErrorResponse)> GetActiveSectionAsync(
        Guid sectionId,
        Guid warehouseId,
        CancellationToken cancellationToken,
        bool includeCoordinates = false)
    {
        var query = _unitOfWork.Sections.Entities
            .Include(s => s.SectionCapacity)
            .AsQueryable();

        if (includeCoordinates)
            query = query.Include(s => s.SectionCoordinates);

        var section = await query.FirstOrDefaultAsync(s =>
            s.Id == sectionId &&
            s.WarehouseId == warehouseId &&
            s.DeletedAt == null &&
            s.IsActive,
            cancellationToken);

        if (section is null)
            return (false, null, _errorManager.ThrowBadRequest<bool>(
                "La sección indicada no existe, está inactiva o no pertenece al almacén.", "ERP:01"));

        return (true, section, default);
    }

    protected async Task<(bool IsValid, bool ErrorResponse)> EnsureWarehouseActiveAsync(
        Guid warehouseId,
        CancellationToken cancellationToken)
    {
        var warehouseExists = await _unitOfWork.Warehouses.Entities
            .AnyAsync(w =>
                w.Id == warehouseId &&
                w.IsActive &&
                w.DeletedAt == null,
                cancellationToken);

        if (!warehouseExists)
            return (false, _errorManager.ThrowBadRequest<bool>(
                "El almacén indicado no existe o no está activo.", "ERP:01"));

        return (true, default);
    }

    protected (bool IsValid, bool ErrorResponse) EnsureSectionHasCoordinates(Sections sections)
    {
        if (sections.SectionCoordinates is null)
            return (false, _errorManager.ThrowBadRequest<bool>(
                "La sección no tiene coordenadas registradas. Regístralas antes de actualizar el layout.",
                "ERP:SECTION_COORDINATES_NOT_FOUND"));

        return (true, default);
    }

    protected async Task<(
        bool IsValid, SectionCapacity? sectionCapacity, WarehouseCapacity? warehouseCapacity, bool ErrorResponse
        )> EnsureCapacitiesAsync(Sections sections, Guid warehouseId, CancellationToken cancellationToken)
    {
        if (sections.SectionCapacity is null)
            return (false, null, null, _errorManager.ThrowBadRequest<bool>(
                "La sección no tiene capacidad registrada.", "ERP:01"));

        var warehouseCapacity = await _unitOfWork.WarehouseCapacities.Entities
               .FirstOrDefaultAsync(c => c.WarehouseId == warehouseId, cancellationToken);

        if (warehouseCapacity is null)
            return (false, null, null, _errorManager.ThrowBadRequest<bool>(
                "El almacén no tiene capacidad registrada", "ERP:01"));

        return (true, sections.SectionCapacity, warehouseCapacity, default);
    }

    protected async Task<(bool IsValid, bool ErrorResponse)> RecalculateSectionAndWarehouseCapacityAsync(
        Sections section,
        Guid warehouseId,
        Guid sectionId,
        decimal? width,
        decimal? length,
        CancellationToken cancellationToken)
    {
        var (capacityOk, sectionCapacity, warehouseCapacity, capsError) =
            await EnsureCapacitiesAsync(section, warehouseId, cancellationToken);

        if (!capacityOk)
            return (false, capsError);

        var effectiveWidth = width ?? sectionCapacity!.Width;
        var effectiveLength = length ?? sectionCapacity!.Length;

        var calculation = await _sectionCapacityCalculator.UpdateSectionAsync(
            sectionId, effectiveWidth, effectiveLength, cancellationToken);

        if (calculation.Section is null || calculation.Warehouse is null)
            return (false, _errorManager.ThrowBadRequest<bool>(
                "No se pudo calcular la capacidad de la sección.", "ERP:01"));

        _mapper.Map(calculation.Section, sectionCapacity);
        await _unitOfWork.SectionCapacities.UpdateAsync(sectionCapacity!);

        _mapper.Map(calculation.Warehouse, warehouseCapacity!);
        await _unitOfWork.WarehouseCapacities.UpdateAsync(warehouseCapacity!);

        return (true, default);
    }

    protected async Task<(bool IsValid, Sections? Section, bool ErrorResponse)> ValidateAccessAndGetSectionAsync(
        Guid userId,
        Guid companyId,
        string moduleCode,
        Guid sectionId,
        Guid warehouseId,
        CancellationToken cancellationToken,
        bool includeCoordinates = false)
    {
        var (accessOk, accessError) = await EnsureAdministratorAccessAsync(
            userId, companyId, moduleCode, cancellationToken);

        if (!accessOk)
            return (false, null, accessError);

        var (sectionOk, section, sectionError) = await GetActiveSectionAsync(
            sectionId, warehouseId, cancellationToken, includeCoordinates);

        if (!sectionOk)
            return (false, null, sectionError);

        var (warehouseOk, warehouseError) = await EnsureWarehouseActiveAsync(
            warehouseId, cancellationToken);

        if (!warehouseOk)
            return (false, null, warehouseError);

        if (includeCoordinates)
        {
            var (sectionCoordinateOk, sectionCoordinateError) = EnsureSectionHasCoordinates(section!);

            if (!sectionCoordinateOk)
                return (false, section, sectionCoordinateError);
        }

        return (true, section, default);
    }
}
