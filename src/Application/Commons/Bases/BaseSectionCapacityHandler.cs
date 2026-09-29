using MediatR;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

namespace ERP.Core.Warehouse.Api.Application.Commons.Bases;

public abstract class BaseSectionCapacityHandler<TRequest>(
    IUnitOfWork unitOfWork,
    IErrorManager errorManager,
    IMapper mapper)
    : BaseValidatorHandler<TRequest, bool>(unitOfWork, errorManager)
    where TRequest : IRequest<bool>
{
    protected readonly IMapper _mapper = mapper;

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

        return (true, section, default);
    }
}
