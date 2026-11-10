using AutoMapper;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Features.InventoryOutputs.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.InventoryOutputs.v1.Queries;

using OutputEntity = ERP.Core.Database.Domain.Entities.Inventory.InventoryOutputs;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryOutputs.v1.Handlers;

public class GetInventoryOutputsDetailHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<GetInventoryOutputsDetailQuery, GetInventoryOutputDetailDto>(unitOfWork, errorManager)
{
    public override async Task<GetInventoryOutputDetailDto> Handle(GetInventoryOutputsDetailQuery request, CancellationToken ct)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, ct);
        if (!access.IsSuccess) return access.ErrorResponse!;

        if (access.Role?.RoleType is not (RoleType.Administrator or RoleType.Supervisor))
            return _errorManager.ThrowUnauthorized<GetInventoryOutputDetailDto>(
                "No tienes acceso a realizar esta acción", "ERP:INVALID_ACCESS");

        var output = await _unitOfWork.InventoryOutputs.Entities
            .Cast<OutputEntity>()
            .AsNoTracking()
            .Include(o => o.Branch)
            .Include(o => o.WorkArea)
            .Include(o => o.CostCenter)
            .Include(o => o.SupplyRequest)
                .ThenInclude(s => s.User)
            .Include(o => o.SupplyRequest)
                .ThenInclude(s => s.WorkArea)
            .Include(o => o.SupplyRequest)
                .ThenInclude(s => s.CostCenter)
            .Include(o => o.SupplyRequest)
                .ThenInclude(s => s.Branch)
            .Include(o => o.SupplyRequest)
                .ThenInclude(s => s.Warehouse)
            .Include(o => o.SupplyRequest)
                .ThenInclude(s => s.OperationalOrder)
            .Include(o => o.SupplyRequest)
                .ThenInclude(s => s.SupplyRequestItems)
                    .ThenInclude(item => item.ProductStock)
                        .ThenInclude(stock => stock.Product)
                            .ThenInclude(product => product.UnitMeasure)
            .Include(o => o.FuelRequest)
                .ThenInclude(f => f.User)
            .Include(o => o.FuelRequest)
                .ThenInclude(f => f.WorkArea)
            .Include(o => o.FuelRequest)
                .ThenInclude(f => f.CostCenter)
            .Include(o => o.FuelRequest)
                .ThenInclude(f => f.Branch)
            .Include(o => o.FuelRequest)
                .ThenInclude(f => f.Machinery)
            .Include(o => o.FuelRequest)
                .ThenInclude(f => f.ProductStock)
                    .ThenInclude(stock => stock.Product)
                        .ThenInclude(product => product.UnitMeasure)
            .Include(o => o.FuelRequest)
                .ThenInclude(f => f.UnitMeasure)
            .Include(o => o.ReasonsInventoryRemoval)
            .Include(o => o.InventoryOutputsItems)
                .ThenInclude(item => item.ProductStock)
                    .ThenInclude(stock => stock.Product)
                        .ThenInclude(product => product.UnitMeasure)
            .FirstOrDefaultAsync(o => o.Id == request.InventoryOutputId && o.DeletedAt == null, ct);

        if (output is null)
            return _errorManager.ThrowNotFound<GetInventoryOutputDetailDto>(
                "La salida de inventario no fue encontrada.", "ERP:INVENTORY_OUTPUT_NOT_FOUND");

        var detail = mapper.Map<GetInventoryOutputDetailDto>(output);

        if (output.FuelRequest != null && output.FuelRequest.ProductStock != null)
        {
            detail.InventoryStockInformations =
            [
                mapper.Map<InventoryStockInformation>(output.FuelRequest.ProductStock)
            ];
        }
        else if (output.SupplyRequest?.SupplyRequestItems.Any(item => item.ProductStock != null) == true)
        {
            detail.InventoryStockInformations = [.. output.SupplyRequest.SupplyRequestItems
                .Where(item => item.ProductStock != null)
                .GroupBy(item => item.ProductStockId)
                .Select(group => mapper.Map<InventoryStockInformation>(group.First().ProductStock!))];
        }

        if (output.DispatchedByUserId.HasValue)
        {
            var dispatchUser = await _unitOfWork.Users.Entities
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == output.DispatchedByUserId.Value, ct);

            detail.DispatchedByUserName = dispatchUser?.Fullname;
        }

        if (detail.SupplyRequestDetail is not null && output.SupplyRequest != null)
        {
            var approvedBy = output.SupplyRequest.ApprovedByUserChiefId.HasValue
                ? await _unitOfWork.Users.Entities
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == output.SupplyRequest.ApprovedByUserChiefId.Value, ct)
                : null;

            var annulledBy = output.SupplyRequest.AnnulledByUserChiefId.HasValue
                ? await _unitOfWork.Users.Entities
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == output.SupplyRequest.AnnulledByUserChiefId.Value, ct)
                : null;

            detail.SupplyRequestDetail.ApprovedByUserChiefName = approvedBy?.Fullname;
            detail.SupplyRequestDetail.AnnulledByUserChiefName = annulledBy?.Fullname;
        }

        if (detail.FuelRequestDetail is not null && output.FuelRequest != null)
        {
            var approvedBy = output.FuelRequest.ApprovedByUserChiefId.HasValue
                ? await _unitOfWork.Users.Entities
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == output.FuelRequest.ApprovedByUserChiefId.Value, ct)
                : null;

            var annulledBy = output.FuelRequest.AnnulledByUserChiefId.HasValue
                ? await _unitOfWork.Users.Entities
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == output.FuelRequest.AnnulledByUserChiefId.Value, ct)
                : null;

            detail.FuelRequestDetail.ApprovedByUserChiefName = approvedBy?.Fullname;
            detail.FuelRequestDetail.AnnulledByUserChiefName = annulledBy?.Fullname;
        }

        return detail;
    }
}