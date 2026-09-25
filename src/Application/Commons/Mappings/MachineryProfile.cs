using AutoMapper;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.Machineries.v1.Commands;
using ERP.Core.Warehouse.Api.Application.Features.Machineries.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{
    public class MachineryProfile : Profile
    {
        public MachineryProfile()
        {
            CreateMap<Machinery, MachineryListDto>();
        }
        public static Machinery ToMachineryEntity(MachineryCommand request, Guid branch)
        {
            return new Machinery
            {
                Id              = Guid.NewGuid(),
                BranchId        = branch,
                Brand           = request.Brand,
                Code            = request.Code,
                Year            = request.Year,
                Model           = request.Model,
                SerialNumber    = request.SerialNumber,
                Status          = MachineryStatus.Available,
                IsActive        = true
            };
        }
    }
}
