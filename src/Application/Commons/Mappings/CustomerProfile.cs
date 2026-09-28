using AutoMapper;
using ERP.Core.Database.Domain.Entities.Bases;
using ERP.Core.Database.Domain.Entities.Operations;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{
    public class CustomerProfile : Profile
    {
        public CustomerProfile()
        {
            CreateMap<Customers, CustomerInformation>()
                .ForMember(dest => dest.CustomerId, opt => opt.MapFrom(src => src.Id));
        }
    }
}