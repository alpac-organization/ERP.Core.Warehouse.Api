using AutoMapper;
using ERP.Core.Database.Domain.Entities.Bases;
using ERP.Core.Database.Domain.Entities.Operations;
using ERP.Core.Warehouse.Api.Application.Features.Customers.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{
    public class CustomerProfile : Profile
    {
        public CustomerProfile()
        {
            CreateMap<Customers, CustomerInformation>()
                .ForMember(dest => dest.CustomerId, opt => opt.MapFrom(src => src.Id));
        
            CreateMap<Customers, CustomerDto>()
                .ForMember(dest => dest.CustomerId, opt => opt.MapFrom(src => src.Id));
        }
    }
}