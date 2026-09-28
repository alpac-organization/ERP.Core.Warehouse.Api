using AutoMapper;
using ERP.Core.Database.Domain.Entities.Operations;
using ERP.Core.Warehouse.Api.Application.Features.OperationalServices.v1.Commands;
using ERP.Core.Warehouse.Api.Application.Features.OperationalServices.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings;

public class OperationalServiceProfile : Profile
{
    public OperationalServiceProfile()
    {
        CreateMap<OperationalService, GetOperationalServiceDto>();
    }
    public static OperationalService ToOperationalServiceEntity(RegisterOperationalServicesCommand request)
    {
        return new OperationalService
        {
            Id = Guid.NewGuid(),
            IsActive = true,
            ServiceCode = request.ServiceCode,
            ServiceName = request.ServiceName,
            Description = request.Description
        };
    }
}