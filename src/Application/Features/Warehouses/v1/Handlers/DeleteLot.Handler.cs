using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers;

public class DeleteLotHandler(IUnitOfWork unitOfWork, IErrorManager errorManager)
    : BaseValidatorHandler<DeleteLotCommand, bool>(unitOfWork, errorManager)
{
    public override async Task<bool> Handle(DeleteLotCommand request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var section = await _unitOfWork.Sections.Entities
            .AsNoTracking()
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

        lot.DeletedAt = DateTime.UtcNow;

        lot.LotsCapacity?.DeletedAt = DateTime.UtcNow;

        foreach (var position in lot.Positions ?? [])
            position.DeletedAt = DateTime.UtcNow;

        await _unitOfWork.Lots.UpdateAsync(lot);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}