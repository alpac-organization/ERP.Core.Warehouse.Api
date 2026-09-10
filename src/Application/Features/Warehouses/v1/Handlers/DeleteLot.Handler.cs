using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;
using ERP.Core.Database.Application.Commons.Interfaces.Services.WarehouseCapacities;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers;

public class DeleteLotHandler(
    IUnitOfWork unitOfWork,
    IErrorManager errorManager,
    AutoMapper.IMapper mapper,
    ILotCapacityCalculator capacityCalculator)
    : LotsCapacityHandlerBase<DeleteLotCommand>(unitOfWork, errorManager, mapper)
{
    public override async Task<bool> Handle(DeleteLotCommand request, CancellationToken cancellationToken)
    {
        var (isValid, section, errorResponse) = await ValidateAccessAndGetSectionAsync(
            request.UserId,
            request.CompanyId,
            request.ModuleCode,
            request.SectionId,
            request.WarehouseId,
            cancellationToken);
        if (!isValid)
            return errorResponse;

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

        await ApplySectionCapacityAsync(section!, calc.Section, cancellationToken);
        await ApplyWarehouseCapacityAsync(section!.WarehouseId, calc.Warehouse, cancellationToken);

        await _unitOfWork.Lots.UpdateAsync(lot);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}