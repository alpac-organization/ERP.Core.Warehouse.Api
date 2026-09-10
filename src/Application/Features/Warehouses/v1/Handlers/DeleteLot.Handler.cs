using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Database.Application.Commons.Interfaces.Services.WarehouseCapacities;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers;

public class DeleteLotHandler(
    IUnitOfWork unitOfWork,
    IErrorManager errorManager,
    IMapper mapper,
    ILotCapacityCalculator capacityCalculator)
    : BaseValidatorHandler<DeleteLotCommand, bool>(unitOfWork, errorManager)
{
    public override async Task<bool> Handle(DeleteLotCommand request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var section = await _unitOfWork.Sections.Entities
            .Include(s => s.SectionCapacity)
            .FirstOrDefaultAsync(
                s => s.Id == request.SectionId && s.IsActive && s.DeletedAt == null,
                cancellationToken);

        if (section is null)
            return _errorManager.ThrowBadRequest<bool>(
                "La sección indicada no existe o no está activa.", "ERP:SECTION_NOT_FOUND");

        if (section.WarehouseId != request.WarehouseId)
            return _errorManager.ThrowBadRequest<bool>(
                "La sección no pertenece al almacén indicado.", "ERP:SECTION_WAREHOUSE_MISMATCH");

        var lot = await _unitOfWork.Lots.Entities
            .Include(l => l.LotsCapacity)
            .FirstOrDefaultAsync(
                l => l.Id == request.LotId
                    && l.SectionId == request.SectionId
                    && l.DeletedAt == null,
                cancellationToken);
        if (lot is null)
            return _errorManager.ThrowBadRequest<bool>(
                "El tramo no fue encontrado.", "ERP:LOT_NOT_FOUND");

        var calc = await capacityCalculator.DeleteLotAsync(request.LotId, cancellationToken);

        lot.DeletedAt = DateTime.UtcNow;

        lot.LotsCapacity?.DeletedAt = DateTime.UtcNow;

        await ApplySectionCapacityAsync(section, calc.Section, cancellationToken);
        await ApplyWarehouseCapacityAsync(section.WarehouseId, calc.Warehouse, cancellationToken);

        await _unitOfWork.Lots.UpdateAsync(lot);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task ApplySectionCapacityAsync(
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

        mapper.Map(calculated, section.SectionCapacity);
        await _unitOfWork.SectionCapacities.UpdateAsync(section.SectionCapacity);
    }

    private async Task ApplyWarehouseCapacityAsync(
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

        mapper.Map(calculated, warehouse.WarehouseCapacity);
        await _unitOfWork.WarehouseCapacities.UpdateAsync(warehouse.WarehouseCapacity);
    }
}