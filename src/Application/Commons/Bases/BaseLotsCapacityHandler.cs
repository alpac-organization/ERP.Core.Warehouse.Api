using MediatR;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

namespace ERP.Core.Warehouse.Api.Application.Commons.Bases;

public abstract class BaseLotsCapacityHandler<TRequest>(
    IUnitOfWork unitOfWork,
    IErrorManager errorManager,
    IMapper mapper)
    : BaseValidatorHandler<TRequest, bool>(unitOfWork, errorManager)
    where TRequest : IRequest<bool>
{
    protected readonly IMapper _mapper = mapper;

    protected async Task<(bool IsValid, Sections? Section, bool ErrorResponse)> ValidateAccessAndGetSectionAsync(
        Guid userId,
        Guid companyId,
        string moduleCode,
        Guid sectionId,
        Guid warehouseId,
        CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(userId, companyId, moduleCode, cancellationToken);
        if (!access.IsSuccess)
            return (false, null, access.ErrorResponse!);

        var section = await _unitOfWork.Sections.Entities
            .Include(s => s.SectionCapacity)
            .FirstOrDefaultAsync(
                s => s.Id == sectionId && s.IsActive && s.DeletedAt == null,
                cancellationToken);

        if (section is null)
            return (false, null, _errorManager.ThrowBadRequest<bool>(
                "La sección indicada no existe o no está activa.", "ERP:SECTION_NOT_FOUND"));

        if (section.WarehouseId != warehouseId)
            return (false, null, _errorManager.ThrowBadRequest<bool>(
                "La sección no pertenece al almacén indicado.", "ERP:SECTION_WAREHOUSE_MISMATCH"));

        return (true, section, default);
    }

    protected async Task<(Lots? Lot, bool IsValid, bool ErrorResponse)> GetExistingLotAsync(
        Guid lotId,
        Guid sectionId,
        CancellationToken cancellationToken)
    {
        var lot = await _unitOfWork.Lots.Entities
            .Include(l => l.LotsCapacity)
            .FirstOrDefaultAsync(
                l => l.Id == lotId
                    && l.SectionId == sectionId
                    && l.DeletedAt == null,
                cancellationToken);
        if (lot is null)
            return (null, false, _errorManager.ThrowBadRequest<bool>(
                "El tramo no fue encontrado.", "ERP:LOT_NOT_FOUND"));

        return (lot, true, default);
    }

    protected async Task<(bool IsValid, bool ErrorResponse)> EnsureLotCodeAvailableAsync(
        string code,
        Guid? excludeLotId,
        Guid sectionId,
        CancellationToken cancellationToken)
    {
        var codeExists = await _unitOfWork.Lots.Entities
            .AnyAsync(
                l => l.SectionId == sectionId
                    && l.Code == code
                    && l.DeletedAt == null
                    && (excludeLotId == null || l.Id != excludeLotId),
                cancellationToken);
        if (codeExists)
            return (false, _errorManager.ThrowBadRequest<bool>(
                $"Ya existe un tramo con el código '{code}' en la sección.",
                "ERP:LOT_CODE_ALREADY_EXISTS"));

        return (true, default);
    }

    protected async Task ApplySectionCapacityAsync(
        Sections section,
        SectionCapacity? calculated,
        CancellationToken cancellationToken)
    {
        if (calculated is null)
            return;

        if (section.SectionCapacity is null)
        {
            calculated.SectionId = section.Id;
            await _unitOfWork.SectionCapacities.RegisterSectionCapacity(calculated);
            return;
        }

        _mapper.Map(calculated, section.SectionCapacity);
        await _unitOfWork.SectionCapacities.UpdateAsync(section.SectionCapacity);
    }

    protected async Task ApplyWarehouseCapacityAsync(
        Guid warehouseId,
        WarehouseCapacity? calculated,
        CancellationToken cancellationToken)
    {
        if (calculated is null)
            return;

        var warehouse = await _unitOfWork.Warehouses.Entities
            .Include(w => w.WarehouseCapacity)
            .FirstOrDefaultAsync(w => w.Id == warehouseId, cancellationToken);
        if (warehouse is null)
            return;

        if (warehouse.WarehouseCapacity is null)
        {
            calculated.WarehouseId = warehouse.Id;
            await _unitOfWork.WarehouseCapacities.RegisterWarehouseCapacity(calculated);
            return;
        }

        _mapper.Map(calculated, warehouse.WarehouseCapacity);
        await _unitOfWork.WarehouseCapacities.UpdateAsync(warehouse.WarehouseCapacity);
    }
}