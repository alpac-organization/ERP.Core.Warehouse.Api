using AutoMapper;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Warehouse.Api.Application.Features.ShippingCompanies.v1.Commands;
using ERP.Core.Warehouse.Api.Application.Features.ShippingCompanies.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings;

public class ShippingCompanyProfile : Profile
{
    public ShippingCompanyProfile()
    {
        // Get
        CreateMap<ShippingCompanies, ShippingCompanyDto>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.Name, o => o.MapFrom(s => s.Name));

        // Create
        CreateMap<RegisterShippingCompanyCommand, ShippingCompanies>()
            .ForMember(d => d.Id, o => o.MapFrom(_ => Guid.NewGuid()))
            .ForMember(d => d.Name, o => o.MapFrom(s => s.Name))
            .ForMember(d => d.DucatRegistries, o => o.Ignore());
    }
}

public static class ShippingCompanyMapper
{
    public static RegisterShippingCompanyCommand ToCommand(
        this RegisterShippingCompanyDto dto,
        Guid userId,
        Guid companyId,
        string moduleCode)
    {
        return new()
        {
            UserId = userId,
            CompanyId = companyId,
            ModuleCode = moduleCode,
            Name = dto.Name
        };
    }
}