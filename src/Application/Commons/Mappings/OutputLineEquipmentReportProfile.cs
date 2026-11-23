using AutoMapper;
using ERP.Core.Database.Domain.Entities.Inventory;
using ERP.Core.Database.Domain.Entities.Operations;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{
    public class OutputLineEquipmentReportProfile : Profile
    {
        public OutputLineEquipmentReportProfile()
        {
            CreateMap<OutputLineEquipmentSource, OutputLineEquipmentItemDto>()
                .ForMember(d => d.OutputCode, o => o.MapFrom(s => s.Output.Code))
                .ForMember(d => d.RequestCode, o => o.MapFrom(s => s.IsFuel
                    ? (s.Output.FuelRequest != null ? s.Output.FuelRequest.Code : null)
                    : (s.Output.SupplyRequest != null ? s.Output.SupplyRequest.Code : null)))
                .ForMember(d => d.OutputDate, o => o.MapFrom(s => s.Output.DispatchedAt ?? s.Output.CreatedAt))
                .ForMember(d => d.Supply, o => o.MapFrom(s => s.IsFuel
                    ? (s.Output.FuelRequest != null && s.Output.FuelRequest.ProductStock != null && s.Output.FuelRequest.ProductStock.Product != null ? s.Output.FuelRequest.ProductStock.Product.ProductName : null)
                    : (s.Item != null && s.Item.ProductStock != null && s.Item.ProductStock.Product != null ? s.Item.ProductStock.Product.ProductName : null)))
                .ForMember(d => d.Quantity, o => o.MapFrom(s => s.IsFuel
                    ? (s.Output.FuelRequest != null ? s.Output.FuelRequest.RequestedQuantity : 0m)
                    : (s.Item != null ? s.Item.Amount : 0m)))
                .ForMember(d => d.Cost, o => o.MapFrom(s => s.IsFuel
                    ? (s.Output.FuelRequest != null && s.Output.FuelRequest.ProductStock != null ? s.Output.FuelRequest.ProductStock.WeightedAverageCost : 0m)
                    : (s.Item != null && s.Item.ProductStock != null ? s.Item.ProductStock.WeightedAverageCost : 0m)))
                .ForMember(d => d.TotalCost, o => o.MapFrom(s =>
                    (s.IsFuel
                        ? (s.Output.FuelRequest != null ? s.Output.FuelRequest.RequestedQuantity : 0m)
                        : (s.Item != null ? s.Item.Amount : 0m)) *
                    (s.IsFuel
                        ? (s.Output.FuelRequest != null && s.Output.FuelRequest.ProductStock != null ? s.Output.FuelRequest.ProductStock.WeightedAverageCost : 0m)
                        : (s.Item != null && s.Item.ProductStock != null ? s.Item.ProductStock.WeightedAverageCost : 0m))))
                .ForMember(d => d.CostCenter, o => o.MapFrom(s => s.Output.CostCenter != null ? s.Output.CostCenter.CostCenterName : null))
                .ForMember(d => d.ServiceOrderCode, o => o.MapFrom(s => !s.IsFuel && s.Output.SupplyRequest != null && s.Output.SupplyRequest.OperationalOrder != null
                    ? s.Output.SupplyRequest.OperationalOrder.PoCode
                    : null))
                .ForMember(d => d.Customer, o => o.MapFrom(s => s.IsFuel
                    ? (s.Output.FuelRequest != null && s.Output.FuelRequest.Customer != null ? s.Output.FuelRequest.Customer.LegalName : null)
                    : (s.Output.SupplyRequest != null && s.Output.SupplyRequest.Customer != null ? s.Output.SupplyRequest.Customer.LegalName : null)));
        }
    }
}