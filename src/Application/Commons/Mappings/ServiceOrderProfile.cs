using AutoMapper;
using ERP.Core.Database.Domain.Entities.Operations;
using ERP.Core.Warehouse.Api.Application.Features.ServiceOrder.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{
    public class ServiceOrderProfile : Profile
    {
        public ServiceOrderProfile()
        {            
            CreateMap<ServicesOrder, ServiceOrderDto>()
                .ForMember(d => d.ServiceOrderId, o => o.MapFrom(s => s.Id));
        }
    }

    public static class ServicesOrderMapper
    {
        
    }
}