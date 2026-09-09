using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;
using ERP.Core.Database.Application.Commons.Interfaces.Services.WarehouseCapacities;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers;

public class RegisterLotHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper,
    ILotCapacityCalculator capacityCalculator)
    : BaseValidatorHandler<RegisterLotCommand, bool>(unitOfWork, errorManager)
{
    public override async Task<bool> Handle(RegisterLotCommand request, CancellationToken cancellationToken)
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

        var lot = mapper.Map<Lots>(request);
        lot.Id = Guid.NewGuid();
        lot.SectionId = section.Id;
        await _unitOfWork.Lots.RegisterLot(lot);

        var calc = await capacityCalculator.CalculateLotAsync(
            section.Id, request.WidthMetres, request.LengthMetres, cancellationToken);

        if (calc.Lot is null || calc.Section is null)
            return _errorManager.ThrowBadRequest<bool>(
                "La sección no tiene capacidad registrada para recalcular.", "ERP:SECTION_CAPACITY_NOT_FOUND");

        var lotCapacity = calc.Lot;
        lotCapacity.LotsId = lot.Id;
        await _unitOfWork.LotsCapacities.RegisterLotsCapacity(lotCapacity);

        await ApplySectionCapacityAsync(section, calc.Section, cancellationToken);
        await ApplyWarehouseCapacityAsync(section.WarehouseId, calc.Warehouse, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task ApplySectionCapacityAsync(
        Sections section,
        SectionCapacity calculated,
        CancellationToken cancellationToken)
    {
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