using AutoMapper;
using ERP.Core.Database.Domain.Entities.Inventory;
using ERP.Core.Database.Domain.Entities.Shopping;
using ERP.Core.Warehouse.Api.Application.Features.InventoryEntries.v1.Dtos;

using Commands = ERP.Core.Warehouse.Api.Application.Features.InventoryEntries.v1.Commands;

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
                .ForMember(dest => dest.PurchaseRequestItems, opt => opt.MapFrom(src =>
                    src.PurchaseOrder.PurchaseRequest != null
                        ? src.PurchaseOrder.PurchaseRequest.PurchaseRequestItems
                        : new List<PurchaseRequestItem>()));

            CreateMap<PurchaseRequestItem, InventoryEntryPurchaseRequestItemDto>()
                .ForMember(dest => dest.PurchaseRequestItemId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product != null ? src.Product.ProductName : null))
                .ForMember(dest => dest.ProductCode, opt => opt.MapFrom(src => src.Product != null ? src.Product.Code : null))
                .ForMember(dest => dest.UnitMeasureName, opt => opt.MapFrom(src => src.UnitMeasure != null ? src.UnitMeasure.Name : null))
                .ForMember(dest => dest.UnitMeasureSymbol, opt => opt.MapFrom(src => src.UnitMeasure != null ? src.UnitMeasure.Symbol : null));
        }
    }

    public static class InventoryEntryMapper
    {
        public static InventoryEntry ToInventoryEntryEntity(this Commands.RegisterInventoryEntryCommand command, string code)
        {
            return new()
            {
                Id = Guid.NewGuid(),
                Code = code,   
                EntryType = command.EntryType
                          
            };
        }
    }
}