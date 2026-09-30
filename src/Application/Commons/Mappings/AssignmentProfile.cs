using AutoMapper;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Operations;

using Commands = ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{

    public class AssignmentProfile: Profile
    {
        public AssignmentProfile()
        {
            CreateMap<AssignmentOperational, AssignmentOperationalDto>()
                .ForMember(dest => dest.AssignmentId, opt => opt.MapFrom(src => src.Id));

            CreateMap<AssignmentOperational, AssignmentOperationalDetailsDto>()
                .IncludeBase<AssignmentOperational, AssignmentOperationalDto>();

        }
    }

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

        public static AssignmentsMachinery ToAssignmentsMachineryEntity(this Commands.AssignedMachinery command, Guid assignmentOperationalId, Guid createByUserId)
        {
            return new()
            {
                Id = Guid.NewGuid(),
                IsActive = true,  
                Concept = command.Concept,
                MachineryId = command.MachineryId,
                CreatedByUserId = createByUserId,
                AssignmentOperationalId = assignmentOperationalId
            };
        }

        public static AssignmentCollaborators ToAssignmentCollaboratorsEntity(this Commands.AssignedCollaborator command, Guid assignmentOperationalId, Guid createdByUserId)
        {   
            return new()
            {
                IsActive = true,
                Id = Guid.NewGuid(),
                Role = command.Role,
                CreatedByUserId = createdByUserId,
                CollaboratorId = command.CollaboratorId,
                AssignmentOperationalId = assignmentOperationalId,
            };
        }
    }
}