using AutoMapper;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Warehouse.Api.Application.Features.ServiceOrder.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.ServiceOrder.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings;

public class ServiceOrderProfile : Profile
{
    public ServiceOrderProfile()
    {
        CreateMap<CreateServiceOrderDto, CreateServiceOrderCommand>();
    }
}