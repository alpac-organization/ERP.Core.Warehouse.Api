using AutoMapper;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Database.Domain.Entities.Operations;

using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Dtos;

using Commands = ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{
    public class AssignmentProfile : Profile
    {
        public AssignmentProfile()
        {
            CreateMap<AssignmentOperational, AssignmentOperationalDto>()
                .ForMember(dest => dest.AssignmentId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.IsAlerted, opt => opt.MapFrom(src => src.OperationalOrder.IsAlerted));

            CreateMap<AssignmentOperational, AssignmentOperationalDetailsDto>()
                .ForPath(dest => dest.WarehouseInformation, opt => opt.MapFrom(src => src.Warehouse))
                .IncludeBase<AssignmentOperational, AssignmentOperationalDto>();

            // Get de asignamientos de maquinaria
            CreateMap<AssignmentsMachinery, GetAssignmentMachineryDto>()
                .ForMember(dest => dest.AssignmentMachineryId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.CreatedByUserName, opt => opt.MapFrom(src => src.User.UserName))
                .ForPath(dest => dest.MachineryInformation.MachineryType, opt => opt.MapFrom(src => src.Machinery.Type))
                .ForPath(dest => dest.MachineryInformation.MachineryCode, opt => opt.MapFrom(src => src.Machinery.Code))
                .ForPath(dest => dest.MachineryInformation.MachineryBrand, opt => opt.MapFrom(src => src.Machinery.Brand));
                
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
                Status = AssignmentOperationalStatus.None,
                Observations = command.Observations,
                Merchandise = command.Merchandise,
                WarehouseId = command.WarehouseId,
                DestinationType = command.DestinationType ?? DestinationType.Warehouse,
                MerchandiseDescription = command.MerchandiseDescription,
                OperationalOrderId = command.OperationalOrderId,
            };
        }
        #endregion

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
                Id = Guid.NewGuid(),
                IsActive = true,
                Role = command.Role,
                CreatedByUserId = createdByUserId,
                CollaboratorId = command.CollaboratorId,
                AssignmentOperationalId = assignmentOperationalId,
            };
        }

        #region Assign Maquinaria
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

        #region Assign Colaboradores
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

        #region POST Asignar posiciones
        public static List<Guid> GetAllPositionIds(this Commands.AssignMerchandiseDesignatedLocationCommand command)
        {
            return [.. command.Sections
                .SelectMany(s => s.Tramos.Concat(s.Racks))
                .SelectMany(b => b.PositionIds)];
        }

        public static Codes ToCodesEntity(this (Guid AssignmentId, CodesType CodeType, (string ImageUrl, string Code) Generated) source)
        {
            return new()
            {
                AssignmentId = source.AssignmentId,
                CodeType = source.CodeType,
                ImageUrl = source.Generated.ImageUrl,
                CodeGenerated = source.Generated.Code
            };
        }

        public static AssignMerchandiseDesignatedLocationDto ToAssignMerchandiseDesignatedLocationDto(this (string CodeQr, string CodeBar) codes)
        {
            return new()
            {
                CodeQr = codes.CodeQr,
                CodeBar = codes.CodeBar
            };
        }

        public static AssignmentStockPlacements ToPlacedStockPlacement(this LotsPositions position, Guid assignmentId, Guid placedByUserId, Guid sectionId)
        {
            return new()
            {
                AssignmentId = assignmentId,
                LotPositionId = position.Id,
                SectionId = sectionId,
                PlacedAt = DateTime.UtcNow,
                PlacedByUserId = placedByUserId
            };
        }

        public static AssignmentStockPlacements ToPlacedStockPlacement(this RackPositions position, Guid assignmentId, Guid placedByUserId, Guid sectionId)
        {
            return new()
            {
                AssignmentId = assignmentId,
                RackPositionId = position.Id,
                SectionId = sectionId,
                PlacedAt = DateTime.UtcNow,
                PlacedByUserId = placedByUserId
            };
        }
        #endregion
    }
}
