using AutoMapper;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Warehouse.Api.Application.Features.ShippingCompanies.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings;

public class ShippingCompanyProfile : Profile
{
    public ShippingCompanyProfile()
    {
        CreateMap<ShippingCompanies, ShippingCompanyDto>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.Name, o => o.MapFrom(s => s.Name));
    }
}