using AutoMapper;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings;

public class LotsProfile : Profile
{
    public LotsProfile()
    {
        CreateMap<RegisterLotCommand, Lots>();
    }
}