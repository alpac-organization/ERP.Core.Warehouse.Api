using AutoMapper;
using ERP.Core.Database.Domain.Entities.Operations;
using ERP.Core.Database.Domain.Entities.Shopping;
using ERP.Core.Warehouse.Api.Application.Features.ServiceOrder.v1.Dtos;

using Commands = ERP.Core.Warehouse.Api.Application.Features.ServiceOrder.v1.Commands.CreateServiceOrderCommand;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{
    public class ServiceOrderProfile : Profile
    {
        public ServiceOrderProfile()
        {            
            CreateMap<ServicesOrder, ServiceOrderDto>()
                .ForMember(d => d.ServiceOrderId, o => o.MapFrom(s => s.Id));
                
            CreateMap<ServiceOrderRequistions, ServiceOrderRequisitionDto>()
                .ForMember(d => d.ServiceOrderRequisitionId, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.ServiceOrderCode, o => o.MapFrom(s => s.ServicesOrder.ServiceOrderCode));
            
        }
    }
    
    public static class ServicesOrderMapper
    {
        public static ServicesOrder ToServiceOrderEntity(this Commands commands, string soCode)
        {
            return new()
            {
                IsActive = true,
                Id = Guid.NewGuid(),
                ServiceOrderCode = soCode,
                Concept = commands.Concept,
                OperationalOrderId = commands.OperationalOrderId,
                OperationalServiceId = commands.OperationalServiceId,
            };
        }
    }
}