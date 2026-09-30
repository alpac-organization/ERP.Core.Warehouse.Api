using AutoMapper;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Operations;

using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Dtos;

using Commands = ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{
    public class AssignmentProfile : Profile
    {
        public AssignmentProfile()
        {
            #region GET Maquinaria
            // Get de asignamientos de maquinaria
            CreateMap<AssignmentsMachinery, GetAssignmentMachineryDto>()
                .ForMember(dest => dest.AssignmentMachineryId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.CreatedByUserName, opt => opt.MapFrom(src => src.User.UserName))
                .ForPath(dest => dest.MachineryInformation.MachineryType, opt => opt.MapFrom(src => src.Machinery.Type))
                .ForPath(dest => dest.MachineryInformation.MachineryCode, opt => opt.MapFrom(src => src.Machinery.Code))
                .ForPath(dest => dest.MachineryInformation.MachineryBrand, opt => opt.MapFrom(src => src.Machinery.Brand));
            #endregion

            #region GET Colaboradores
            // get de asignamiento de colaboradores
            CreateMap<AssignmentCollaborators, GetAssignmentCollaboratorsDto>()
                .ForMember(dest => dest.AssignmentCollaboratorId, opt => opt.MapFrom(src => src.Id))
                .ForPath(dest => dest.CollaboratorInformation.CollaboratorName, opt => opt.MapFrom(src => string.Join(" ",
                    new[]
                    {
                        src.Collaborator.FirstName,
                        src.Collaborator.SecondName,
                        src.Collaborator.FirstLastname,
                        src.Collaborator.SecondLastname
                    }.Where(part => !string.IsNullOrWhiteSpace(part)))))
                .ForMember(dest => dest.CreatedByUserName, opt => opt.MapFrom(src => src.User.UserName))
                .ForPath(dest => dest.CollaboratorInformation.WorkAreaName, opt => opt.MapFrom(src => src.Collaborator.WorkingInformation.Area.WorkAreaName))
                .ForPath(dest => dest.CollaboratorInformation.JobPositionName, opt => opt.MapFrom(src => src.Collaborator.WorkingInformation.JobPosition.JobPositionName));
            #endregion
        }
    }

    public static class AssignmentMapper
    {
        #region POST Asignación operativa
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
        #endregion

        #region POST Maquinaria
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
        #endregion

        #region POST Colaboradores
        public static AssignmentCollaborators ToAssignmentCollaboratorsEntity(this Commands.AssignedCollaborator command, Guid assignmentOperationalId)
        {
            return new()
            {
                Id = Guid.NewGuid(),
                IsActive = true,
                Role = command.Role,
                CollaboratorId = command.CollaboratorId,
                AssignmentOperationalId = assignmentOperationalId,
            };
        }
        #endregion

        #region POST Maquinaria
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

        #region POST Colaboradores
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
