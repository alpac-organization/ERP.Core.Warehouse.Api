using AutoMapper;
using ERP.Core.Database.Domain.Entities.Inventory;
using ERP.Core.Warehouse.Api.Application.Features.SupplyRequests.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings;

public class SupplyRequestsProfile : Profile
{
    public SupplyRequestsProfile()
    {
        CreateMap<SupplyRequest, GetSupplyRequestDto>();
    }
}