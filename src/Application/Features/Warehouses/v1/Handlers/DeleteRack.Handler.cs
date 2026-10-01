using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;
using ERP.Core.Database.Application.Commons.Interfaces.Services.WarehouseCapacities;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers;

public class DeleteRackHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, AutoMapper.IMapper mapper, IRackCapacityCalculator capacityCalculator) : BaseRacksCapacityHandler<DeleteRackCommand>(unitOfWork, errorManager, mapper)
{
    public override async Task<bool> Handle(DeleteRackCommand request, CancellationToken cancellationToken)
    {
        var (isValid, section, errorResponse) = await ValidateAccessAndGetSectionAsync(
            request.UserId, request.CompanyId, request.ModuleCode, request.SectionId,
            request.WarehouseId, cancellationToken);
        if (!isValid) return errorResponse;

        var (rackCandidate, rackIsValid, rackError) = await GetExistingRackAsync(
            request.RackId, request.SectionId, cancellationToken);
        if (!rackIsValid) return rackError;

        var rack = rackCandidate!;

        var activePositions = rack.Positions?
            .Where(p => p.DeletedAt == null)
            .ToList() ?? [];

        ///Agregar metodo para validar si existe mercaderia en el tramo.
        // await ValidateNoActiveStockAsync(activePositions, cancellationToken);

        var calc = await capacityCalculator.DeleteRackAsync(rack.Id, cancellationToken);

        foreach (var position in activePositions)
        {
            position.DeletedAt = DateTime.UtcNow;
            await _unitOfWork.RackPositions.UpdateAsync(position);
        }

        rack.DeletedAt = DateTime.UtcNow;

        if (rack.RackCapacity != null)
            rack.RackCapacity.DeletedAt = DateTime.UtcNow;

        if (rack.RacksCoordinates != null)
            rack.RacksCoordinates.DeletedAt = DateTime.UtcNow;

        await ApplySectionCapacityAsync(section!, calc.Section, cancellationToken);
        await ApplyWarehouseCapacityAsync(section!.WarehouseId, calc.Warehouse, cancellationToken);

        await _unitOfWork.Racks.UpdateAsync(rack);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
