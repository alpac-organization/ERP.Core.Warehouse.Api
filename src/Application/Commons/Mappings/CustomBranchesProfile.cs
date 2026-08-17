using AutoMapper;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Warehouse.Api.Application.Features.CustomBranches.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings;

public class CustomBranchesProfile : Profile
{
    public CustomBranchesProfile()
    {
        CreateMap<CustomsBranches, CustomBranchListItemDto>();
    }
}