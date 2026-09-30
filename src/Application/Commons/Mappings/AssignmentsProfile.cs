using AutoMapper;
using ERP.Core.Database.Domain.Entities.Operations;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Dtos;
using Commands = ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{
    public class AssignmentsProfile : Profile
    {
        public AssignmentsProfile()
        {
            // Get de asignamientos de maquinaria
            CreateMap<AssignmentsMachinery, GetAssignmentMachineryDto>()
                .ForMember(dest => dest.AssignmentMachineryId, opt => opt.MapFrom(src => src.Id));

            // get de asignamiento de colaboradores
            CreateMap<AssignmentCollaborators, GetAssignmentCollaboratorsDto>()
                .ForMember(dest => dest.AssignmentCollaboratorId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.CollaboratorName, opt => opt.MapFrom(src => string.Join(" ",
                    new[]
                    {
                        src.Collaborator.FirstName,
                        src.Collaborator.SecondName,
                        src.Collaborator.FirstLastname,
                        src.Collaborator.SecondLastname
                    }.Where(part => !string.IsNullOrWhiteSpace(part)))))
                .ForMember(dest => dest.CreatedByUserName, opt => opt.MapFrom(src => src.User.UserName));
        }
    }

    public static class AssignmentsMappingExtensions
    {
        #region Post Maquinaria
        public static List<AssignmentsMachinery> ToAssignmentsMachineryEntities(
            this Commands.CreateAssignmentMachineryCommand command)
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

        #region Post Colaboradores
        public static List<AssignmentCollaborators> ToAssignmentCollaboratorsEntities(
            this Commands.CreateAssignmentCollaboratorsCommand command,
            Guid operationalOrderId)
        {
            return [.. command.Collaborators
                .Distinct()
                .Select(collaboratorId => new AssignmentCollaborators
                {
                    Id = Guid.NewGuid(),
                    IsActive = true,
                    CreatedByUserId = command.UserId,
                    CollaboratorId = collaboratorId,
                    Role = command.Role,
                    AssignmentOperationalId = command.AssignmentOperationalId
                })];
        }
        #endregion
    }
}