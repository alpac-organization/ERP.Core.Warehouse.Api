using AutoMapper;
using ERP.Core.Database.Domain.Entities.Operations;
using ERP.Core.Warehouse.Api.Application.Features.AssignmentMachineries.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings;

public class AssignmentMachineryProfile : Profile
{
    public static AssignmentsMachinery ToAssignmentMachineryEntity(CreateAssignmentMachineryCommand request)
    {
        return new AssignmentsMachinery
        {
            Id = Guid.NewGuid(),
            OperationalOrderId = request.OperationalOrderId,
            MachineryId = request.MachineryId,
            IsActive = true
        };
    }
}