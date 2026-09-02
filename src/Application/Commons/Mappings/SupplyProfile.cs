using AutoMapper;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Warehouse.Api.Application.Features.Supplies.v1.Commands;
using ERP.Core.Warehouse.Api.Application.Features.Supplies.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings;

public class SupplyProfile : Profile
{
    public SupplyProfile()
    {
        // Get
        CreateMap<Supplies, SupplyDto>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.Name, o => o.MapFrom(s => s.Name));

        // Create
        CreateMap<RegisterSupplyCommand, Supplies>()
            .ForMember(d => d.Id, o => o.MapFrom(_ => Guid.NewGuid()))
            .ForMember(d => d.Name, o => o.MapFrom(s => s.Name))
            .ForMember(d => d.Description, o => o.MapFrom(s => s.Description))
            .ForMember(d => d.UnloadingSupplies, o => o.Ignore());
    }
}

public static class SupplyMapper
{
    public static RegisterSupplyCommand ToCommand(
        this RegisterSupplyDto dto,
        Guid userId,
        Guid companyId,
        string moduleCode)
    {
        return new()
        {
            UserId = userId,
            CompanyId = companyId,
            ModuleCode = moduleCode,
            Name = dto.Name,
            Description = dto.Description
        };
    }
}
