using AutoMapper;
using ERP.Core.Database.Domain.Entities.Warehouse;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings;

public class CustomerProfile : Profile
{
    public CustomerProfile()
    {
        CreateMap<Customer, CustomerInformation>()
            .ForMember(dest => dest.CustomerId, opt => opt.MapFrom(src => src.Id));
    }
}
