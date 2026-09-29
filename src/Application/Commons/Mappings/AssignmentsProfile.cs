using ERP.Core.Database.Domain.Entities.Operations;

using Commands = ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{
    public static class AssignmentsMapper
    {
        #region Maquinaria

        public static List<AssignmentsMachinery> ToAssignmentsMachineryEntities(this Commands.CreateAssignmentMachineryCommand command)
        {
            return [.. command.Machinery
                .Distinct()
                .Select(machineryId => new AssignmentsMachinery
                {
                    Id = Guid.NewGuid(),
                    IsActive = true,
                    Concept = command.Concept,
                    CreatedByUserId = command.UserId,
                    MachineryId = machineryId,
                    AssignmentOperationalId = command.AssignmentOperationalId
                })];
        }

        #endregion

        #region Colaboradores

        public static List<AssignmentCollaborators> ToAssignmentCollaboratorsEntities(this Commands.CreateAssignmentCollaboratorsCommand command, Guid operationalOrderId)
        {
            return [.. command.Collaborators
                .Distinct()
                .Select(collaboratorId => new AssignmentCollaborators
                {
                    Id = Guid.NewGuid(),
                    IsActive = true,
                    CreatedByUserId = command.UserId,
                    OperationalOrderId = operationalOrderId,
                    CollaboratorId = collaboratorId,
                    Role = command.Role,
                    AssignmentOperationalId = command.AssignmentOperationalId
                })];
        }

        #endregion
    }
}
