using AutoMapper;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Warehouse.Api.Application.Features.ServiceOrder.v1.Commands;
using ERP.Core.Warehouse.Api.Application.Features.ServiceOrder.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings;

public class ServiceOrderProfile : Profile
{
    public ServiceOrderProfile()
    {
        CreateMap<CreateServiceOrderDto, CreateServiceOrderCommand>();
        CreateMap<CreateServiceOrderCommand, ServiceOrder>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Code, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore());

        CreateMap<ServiceOrder, CreateServiceOrderResponse>()
            .ForCtorParam(nameof(CreateServiceOrderResponse.ServiceOrderId), opt => opt.MapFrom(src => src.Id));

        CreateMap<ServiceOrder, ServiceOrderDto>()
            .ForMember(dest => dest.ServiceOrderId, opt => opt.MapFrom(src => src.Id));    }
}