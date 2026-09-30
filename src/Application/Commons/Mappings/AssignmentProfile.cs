using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Operations;

using Commands = ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{
    public static class AssignmentMapper
    {
        public static AssignmentOperational ToAssignmentOperationalEntity(this Commands.CreateAssignmentCommand command)
        {
            return new()
            {
                Id = Guid.NewGuid(),
                IsActive = true,
                Status = AssignmentOperationalStatus.Pending,
                Observations = command.Observations,
                Merchandise = command.Merchandise,
                WarehouseId = command.WarehouseId,
                DestinationType = command.DestinationType ?? DestinationType.Warehouse,
                MerchandiseDescription = command.MerchandiseDescription,
                OperationalOrderId = command.OperationalOrderId,
            };
        }

        public static AssignmentsMachinery ToAssignmentsMachineryEntity(this Commands.AssignedMachinery command, Guid assignmentOperationalId)
        {
            return new()
            {
                Id = Guid.NewGuid(),
                IsActive = true,  
                Concept = command.Concept,
                MachineryId = command.MachineryId,
                AssignmentOperationalId = assignmentOperationalId
            };
        }

        public static AssignmentCollaborators ToAssignmentCollaboratorsEntity(this Commands.AssignedCollaborator command, Guid assignmentOperationalId)
        {   
            return new()
            {
                IsActive = true,
                Id = Guid.NewGuid(),
                Role = command.Role,
                CollaboratorId = command.CollaboratorId,
                AssignmentOperationalId = assignmentOperationalId,
            };
        }
    }
}