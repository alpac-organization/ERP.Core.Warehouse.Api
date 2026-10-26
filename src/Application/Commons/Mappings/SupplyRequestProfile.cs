using AutoMapper;
using ERP.Core.Database.Domain.Entities.Inventory;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.SupplyRequests.v1.Dtos;
using Commands = ERP.Core.Warehouse.Api.Application.Features.SupplyRequests.v1.Commands;
using DbUserProfile = ERP.Core.Database.Domain.Entities.Auth.UserProfile;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings;

public class SupplyRequestsProfile : Profile
{
    public SupplyRequestsProfile()
    {
        CreateMap<SupplyRequest, GetSupplyRequestDto>();
    }
}

public static class SupplyRequestMapper
{
    public static SupplyRequest ToSupplyRequestEntity(
        this Commands.RegisterSupplyRequestsCommad request, DbUserProfile userProfile, string code,
        Guid warehouseId
    )
    {
        var id = Guid.NewGuid();
        return new()
        {
            Id                  = id,
            Code                = code,
            Justification       = request.Justification,
            OutflowStatus       = OutflowStatus.PendingReview,
            WorkAreaId          = userProfile.AreaId,
            CostCenterId        = userProfile.CostCenterId,
            BranchId            = userProfile.BranchId,
            WarehouseId         = warehouseId,
            OperationalOrderId  = request.OperationalOrderId,
            RequestedByUserId   = userProfile.UserId,
            SupplyRequestItems  = [.. request.Items.Select(item => item.ToSupplyRequestItemEntity(id))]
        };
    }

    public static SupplyRequestItem ToSupplyRequestItemEntity(
        this Commands.RegisterSupplyRequestItem command, Guid supplyRequestId)
    {
        return new()
        {
            Id                  = Guid.NewGuid(),
            IsActive            = true,
            ProductStockId      = command.ProductStockId,
            Amount              = command.Amount,
            ReasonUse           = ReasonUse.Operation,
            SupplyRequestId     = supplyRequestId
        };
    }
}