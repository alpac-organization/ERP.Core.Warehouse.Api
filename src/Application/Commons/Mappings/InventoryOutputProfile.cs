using AutoMapper;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Auth;
using ERP.Core.Database.Domain.Entities.Bases;
using ERP.Core.Database.Domain.Entities.Inventory;

using ERP.Core.Warehouse.Api.Application.Features.InventoryOutputs.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.SupplyRequests.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.FuelRequests.v1.Dtos;
using Commands = ERP.Core.Warehouse.Api.Application.Features.ReviewSupplyRequest.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{
    public class InventoryOutputProfile : Profile
    {
        public InventoryOutputProfile()
        {
            CreateMap<InventoryOutputs, GetInventoryOutputDto>()
                .ForMember(dest => dest.InventoryOutputId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.OutputCode, opt => opt.MapFrom(src => src.Code))
                .ForMember(dest => dest.RequestCode, opt => opt.MapFrom(src =>
                    src.SupplyRequest != null ? src.SupplyRequest.Code : src.FuelRequest != null ? src.FuelRequest.Code : null))
                .ForMember(dest => dest.RequestType, opt => opt.MapFrom(src =>
                    src.SupplyRequestId.HasValue ? "Supply" : src.FuelRequestId.HasValue ? "Fuel" : null))
                .ForMember(dest => dest.ReasonsInventoryRemovalDescription, opt => opt.MapFrom(src => src.ReasonsInventoryRemoval.Description))
                .ForMember(dest => dest.WorkAreaName, opt => opt.MapFrom(src => src.WorkArea.WorkAreaName))
                .ForMember(dest => dest.CostCenterName, opt => opt.MapFrom(src => src.CostCenter.CostCenterName))
                .ForMember(dest => dest.RequestDate, opt => opt.MapFrom(src =>
                    src.SupplyRequest != null && src.FuelRequest == null
                        ? (DateTime?)src.SupplyRequest.CreatedAt
                        : src.FuelRequest != null
                            ? (DateTime?)src.FuelRequest.CreatedAt
                            : null));

            CreateMap<InventoryOutputs, GetInventoryOutputDetailDto>()
                .ForMember(dest => dest.SupplyRequestDetail, opt => opt.MapFrom(src => src.SupplyRequest))
                .ForMember(dest => dest.FuelRequestDetail, opt => opt.MapFrom(src => src.FuelRequest))
                .ForMember(dest => dest.InventoryStockInformations, opt => opt.Ignore());
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

        public static InventoryOutputs ToInventoryOutputEntity(this Commands.ApproveSupplyRequestCommand request,
            FuelRequest fuelRequest, string code)
        {
            return new InventoryOutputs
            {
                Id = Guid.NewGuid(),
                Code = code,
                Observations = request.Observations,
                Status = InventoryOutputsStatus.OutputPending,
                BranchId = fuelRequest.BranchId,
                WorkAreaId = fuelRequest.WorkAreaId,
                CostCenterId = fuelRequest.CostCenterId,
                FuelRequestId = fuelRequest.Id,
                ReasonsInventoryRemovalId = request.ReasonInventoryRemovalId
            };
        }

        public static InventoryOutputsItem ToInventoryOutputItemEntity(Guid inventoryOutputId, Guid productStockId, decimal dispatchedQuantity)
        {
            return new InventoryOutputsItem
            {
                Id = Guid.NewGuid(),
                InventoryOutputId = inventoryOutputId,
                ProductStockId = productStockId,
                DispatchedQuantity = dispatchedQuantity
            };
        }

        public static Kardex ToKardexEntity(
            InventoryOutputsItem item, ProductStock stock, InventoryOutputs output,
            Guid userId, DateTime now)
        {
            var previousUnits = stock.AvailableQuantity + item.DispatchedQuantity;
            var previousCost = stock.WeightedAverageCost;
            var previousKardex = previousUnits * previousCost;

            var dispatched = item.DispatchedQuantity;
            var unitCost = previousCost;
            var totalOutput = dispatched * unitCost;

            var newUnits = stock.AvailableQuantity;
            var newKardex = previousKardex - totalOutput;
            var newAvgCost = newUnits > 0 ? newKardex / newUnits : previousCost;

            return new Kardex
            {
                Id = Guid.NewGuid(),
                KardexType = KardexType.Operational,
                MovementType = KardexMovementType.Exit,
                Units = dispatched,
                UnitCost = unitCost,
                TotalInput = 0,
                TotalOutput = totalOutput,
                TotalUnits = newUnits,
                AverageCost = newAvgCost,
                TotalKardex = newUnits * newAvgCost,
                TransactionDate = DateOnly.FromDateTime(now),
                TransactionTime = TimeOnly.FromDateTime(now),
                ProductId = stock.ProductId,
                MovementByUserId = userId,
                WorkAreaId = output.WorkAreaId,
                CostCenterId = output.CostCenterId
            };
        }
    }
}