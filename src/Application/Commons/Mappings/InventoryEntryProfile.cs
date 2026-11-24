using AutoMapper;
using ERP.Core.Database.Domain.Entities.Shopping;
using ERP.Core.Database.Domain.Entities.Inventory;
using ERP.Core.Warehouse.Api.Application.Features.InventoryEntries.v1.Dtos;

using PurchaseOrderCommands = ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Commands;
using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{
    public class InventoryEntryProfile : Profile
    {
        public InventoryEntryProfile()
        {
            CreateMap<InventoryEntry, InventoryEntryDto>()
                .ForMember(dest => dest.InventoryEntryId,   opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.BranchInformation,   opt => opt.MapFrom(src => src.Branch))
                .ForMember(dest => dest.WorkAreaInformation,   opt => opt.MapFrom(src => src.WorkArea))
                .ForMember(dest => dest.PurchaseRequestCode,   opt => opt.MapFrom(src => src.PurchaseOrder.PurchaseRequest.Code));

            CreateMap<InventoryEntry, InventoryEntryDetailDto>()
                .IncludeBase<InventoryEntry, InventoryEntryDto>()
                .ForMember(dest => dest.WorkAreaInformation, opt => opt.MapFrom(src => src.WorkArea))
                .ForMember(dest => dest.PurchaseRequestInformation, opt => opt.MapFrom(src => src.PurchaseOrder.PurchaseRequest))
                .ForMember(dest => dest.CostCenterInformation, opt => opt.MapFrom(src =>
                    src.PurchaseOrder != null && src.PurchaseOrder.PurchaseRequest != null
                        ? src.PurchaseOrder.PurchaseRequest.CostCenter
                        : null))
                .ForMember(dest => dest.PurchaseRequestItems, opt => opt.MapFrom(src =>
                    src.PurchaseOrder.PurchaseRequest != null
                        ? src.PurchaseOrder.PurchaseRequest.PurchaseRequestItems
                        : new List<PurchaseRequestItem>()));

            CreateMap<PurchaseRequestItem, InventoryEntryPurchaseRequestItemDto>()
                .ForMember(dest => dest.PurchaseRequestItemId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product != null ? src.Product.ProductName : null))
                .ForMember(dest => dest.ProductCode, opt => opt.MapFrom(src => src.Product != null ? src.Product.Code : null))
                .ForMember(dest => dest.UnitMeasureName, opt => opt.MapFrom(src => src.UnitMeasure != null ? src.UnitMeasure.Name : null))
                .ForMember(dest => dest.UnitMeasureSymbol, opt => opt.MapFrom(src => src.UnitMeasure != null ? src.UnitMeasure.Symbol : null))
                .ForMember(dest => dest.CostCenterId, opt => opt.MapFrom(src =>
                    src.Product != null && src.Product.CostCenterId.HasValue
                        ? src.Product.CostCenterId
                        : (src.PurchaseRequest != null ? src.PurchaseRequest.CostCenterId : null)))
                .ForMember(dest => dest.CostCenterName, opt => opt.MapFrom(src =>
                    src.Product != null && src.Product.CostCenter != null
                        ? src.Product.CostCenter.CostCenterName
                        : (src.PurchaseRequest != null && src.PurchaseRequest.CostCenter != null ? src.PurchaseRequest.CostCenter.CostCenterName : null)))
                .ForMember(dest => dest.CostCenterCode, opt => opt.MapFrom(src =>
                    src.Product != null && src.Product.CostCenter != null
                        ? src.Product.CostCenter.CostCenterCode
                        : (src.PurchaseRequest != null && src.PurchaseRequest.CostCenter != null ? src.PurchaseRequest.CostCenter.CostCenterCode : null)))
                .ForMember(dest => dest.CostCenterInformation, opt => opt.MapFrom(src =>
                    src.Product != null && src.Product.CostCenter != null
                        ? src.Product.CostCenter
                        : (src.PurchaseRequest != null ? src.PurchaseRequest.CostCenter : null)));
        }
    }
    
    public static class InventoryEntryMapper
    {
        public static InventoryEntry ToInventoryEntity(this PurchaseOrderCommands.ProcessPurchaseOrderEntryCommand command)
        {
            return new()
            {
                EntryType    = EntryType.Buy,
                EntryStatus  = EntryStatus.PendingReview,
                Observations = command.Observations
            };
        }
    }

}