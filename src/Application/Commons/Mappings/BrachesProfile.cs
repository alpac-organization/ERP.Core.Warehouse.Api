using AutoMapper;
using ERP.Core.Database.Domain.Entities.Bases;
using ERP.Core.Database.Domain.Entities.Catalogs;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{
    public class BranchesProfile : Profile
    {
        public BranchesProfile()
        {
            CreateMap<Branch, BranchInformation>()
                .ForMember(dest => dest.BranchId, opt => opt.MapFrom(src => src.Id)); 
        }
    }
}