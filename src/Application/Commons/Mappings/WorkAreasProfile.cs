using AutoMapper;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.RequisitionAccountingReviews.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{
    public class WorkAreasProfile : Profile
    {
        public WorkAreasProfile()
        {
            CreateMap<WorkArea, WorkAreaInformation>()
                .ForMember(dest => dest.WorkAreaId, opt => opt.MapFrom(src => src.Id));
        }
    }
}