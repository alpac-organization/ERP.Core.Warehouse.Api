using AutoMapper;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Warehouse.Api.Application.Features.RequisitionAccountingReviews.v1.Dtos;

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