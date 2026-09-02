using System;
using AutoMapper;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{
    public class WarehouseAssignmentsProfile : Profile
    {
        public WarehouseAssignmentsProfile()
        {
            CreateMap<AssignWarehouseDto, CreateWarehouseAssignmentCommand>();
            CreateMap<AssignUnloadingCrewDto, CreateUnloadingCrewCommand>();
            CreateMap<AssignUnloadingMachineryDto, CreateUnloadingMachineryCommand>();
            CreateMap<CompleteWarehouseAssignmentDto, CompleteWarehouseAssignmentCommand>();
        }
    }

    public static class WarehouseAssignmentsMapper
    {
        public static CreateWarehouseAssignmentCommand ToCommand(
            this AssignWarehouseDto dto, Guid receptionId, Guid userId, Guid companyId, string moduleCode)
        {
            return new CreateWarehouseAssignmentCommand
            {
                ReceptionId = receptionId,
                UserId = userId,
                CompanyId = companyId,
                ModuleCode = moduleCode,
                EntranceDucatId = dto.EntranceDucatId,
                WarehouseId = dto.WarehouseId,
                WarehouseChiefUserId = dto.WarehouseChiefUserId
            };
        }

        public static CreateUnloadingCrewCommand ToCommand(
            this AssignUnloadingCrewDto dto, Guid receptionId, Guid userId, Guid companyId, string moduleCode)
        {
            return new CreateUnloadingCrewCommand
            {
                ReceptionId = receptionId,
                EntranceDucatId = dto.EntranceDucatId,
                UserId = userId,
                CompanyId = companyId,
                ModuleCode = moduleCode,
                CollaboratorIds = dto.CollaboratorIds,
                IsOutsourced = dto.IsOutsourced,
                PersonCount = dto.PersonCount,
                ProviderName = dto.ProviderName?.Trim(),
                InvoiceNumber = dto.InvoiceNumber?.Trim()
            };
        }

        public static CreateUnloadingMachineryCommand ToCommand(
            this AssignUnloadingMachineryDto dto, Guid receptionId, Guid userId, Guid companyId, string moduleCode)
        {
            return new CreateUnloadingMachineryCommand
            {
                ReceptionId = receptionId,
                EntranceDucatId = dto.EntranceDucatId,
                UserId = userId,
                CompanyId = companyId,
                ModuleCode = moduleCode,
                MachineryCode = dto.MachineryCode ?? string.Empty,
                OperatorCollaboratorId = dto.OperatorCollaboratorId,
                IsOutsourced = dto.IsOutsourced,
                StartTime = dto.StartTime,
                ProviderName = dto.ProviderName ?? string.Empty,
                InvoiceNumber = dto.InvoiceNumber ?? string.Empty,
                MachineryDescription = dto.MachineryDescription ?? string.Empty
            };
        }
    }
}
