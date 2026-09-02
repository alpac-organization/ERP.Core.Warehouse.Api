using System;
using AutoMapper;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseMachineries.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseMachineries.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{
    public class WarehouseMachineriesProfile : Profile
    {
        public WarehouseMachineriesProfile()
        {
            CreateMap<CreateWarehouseMachineryDto, CreateWarehouseMachineryCommand>();
            
            CreateMap<CreateWarehouseMachineryCommand, WarehouseMachinery>()
                .ForMember(d => d.Id, opt => opt.MapFrom(_ => Guid.NewGuid()))
                .ForMember(d => d.IsActive, opt => opt.MapFrom(_ => true));

            CreateMap<WarehouseMachinery, WarehouseMachineryListDto>()
                .ConstructUsing(m => new WarehouseMachineryListDto(
                    m.Id,
                    m.Code,
                    m.SerialNumber,
                    m.LicensePlate,
                    m.Name,
                    m.Brand,
                    m.Model,
                    m.MachineryType.ToString(),
                    m.FuelType.ToString(),
                    m.Status.ToString()
                ));
        }
    }

    public static class WarehouseMachineriesMapper
    {
        public static CreateWarehouseMachineryCommand ToCommand(
            this CreateWarehouseMachineryDto dto, Guid userId, Guid companyId, string moduleCode, IMapper mapper)
        {
            var command = mapper.Map<CreateWarehouseMachineryCommand>(dto);
            command.UserId = userId;
            command.CompanyId = companyId;
            command.ModuleCode = moduleCode;
            return command;
        }
    }
}
