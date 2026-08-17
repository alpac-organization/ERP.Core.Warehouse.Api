using AutoMapper;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Warehouse.Api.Application.Features.ServiceOrder.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings;

public class CustomerProfile : Profile
{
    public CustomerProfile()
    {
        CreateMap<Customer, CustomerInformation>()
            .ForMember(dest => dest.CustomerId, opt => opt.MapFrom(src => src.Id));
    }
}
