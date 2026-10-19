using AutoMapper;
using ERP.Core.Database.Domain.Entities.Inventory;
using ERP.Core.Warehouse.Api.Application.Features.InventoryEntries.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{
    public class InventoryEntryProfile : Profile
    {
        public InventoryEntryProfile()
        {
            CreateMap<InventoryEntry, InventoryEntryDto>()
                .ForMember(dest => dest.InventoryEntryId,   opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.BranchInformation,   opt => opt.MapFrom(src => src.Branch));
        }
    }
}