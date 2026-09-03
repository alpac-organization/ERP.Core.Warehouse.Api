using AutoMapper;
using ERP.Core.Database.Domain.Entities.Bases;
using ERP.Core.Database.Domain.Entities.Catalogs;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{
    public class CostCenterProfile : Profile
    {
        public CostCenterProfile()
        {
            CreateMap<CostCenter, CostCenterInformation>()
                .ForMember(dest => dest.CostCenterId, opt => opt.MapFrom(src => src.Id)); 
        }
    }
}