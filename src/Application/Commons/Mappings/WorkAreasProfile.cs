using AutoMapper;
using ERP.Core.Database.Domain.Entities.Bases;
using ERP.Core.Database.Domain.Entities.Catalogs;
namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{
    public class WorkAreasProfile : Profile
    {
        public WorkAreasProfile()
        {
            CreateMap<WorkArea, WorkAreaInformation>()
                .ForMember(dest => dest.WorkAreaId, opt => opt.MapFrom(src => src.Id))
                .ForPath(dest => dest.CostCenters, opt => opt.MapFrom(src => src.CostCenters));
        }
    }
}