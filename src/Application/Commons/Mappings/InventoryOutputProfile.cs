using AutoMapper;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Inventory;

using Commands = ERP.Core.Warehouse.Api.Application.Features.ReviewSupplyRequest.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{
    public class InventoryOutputProfile : Profile
    {
        public InventoryOutputProfile()
        {
        }
    }

    public static class InventoryOutputMapper
    {
        public static InventoryOutputs ToInventoryOutputEntity(this Commands.ApproveSupplyRequestCommand request,
            SupplyRequest supplyRequest, string code)
        {
            return new InventoryOutputs
            {
                Id = Guid.NewGuid(),
                Code = code,
                Observations = request.Observations,
                Status = InventoryOutputsStatus.OutputPending,
                BranchId = supplyRequest.BranchId,
                WorkAreaId = supplyRequest.WorkAreaId,
                CostCenterId = supplyRequest.CostCenterId,
                SupplyRequestId = supplyRequest.Id,
                ReasonsInventoryRemovalId = request.ReasonInventoryRemovalId
            };
        }
    }
}
