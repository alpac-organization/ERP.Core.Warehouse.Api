using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Commands;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;

namespace ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Handlers
{
    public class CreateUnloadingCrewHandler : BaseValidatorHandler<CreateUnloadingCrewCommand, bool>
    {
        public CreateUnloadingCrewHandler(IUnitOfWork unitOfWork, IErrorManager errorManager)
            : base(unitOfWork, errorManager)
        {
        }

        public override async Task<bool> Handle(CreateUnloadingCrewCommand request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
            if (!access.IsSuccess) return access.ErrorResponse!;

            var assignment = await WarehouseAssignmentRules.GetActiveAssignmentAsync(
                _unitOfWork, request.ReceptionId, request.EntranceDucatId, cancellationToken);

            if (assignment == null)
            {
                return _errorManager.ThrowBadRequest<bool>(
                    "Debe asignar primero una bodega antes de asignar la cuadrilla.",
                    "ERP:ASSIGNMENT_NOT_FOUND");
            }

            var assignResult = request.IsOutsourced
                ? await AssignOutsourcedCrewAsync(assignment.Id, request)
                : await AssignInternalCrewAsync(assignment.Id, request, cancellationToken);

            if (!assignResult) return false;

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }

        private async Task<bool> AssignInternalCrewAsync(
            Guid assignmentId, CreateUnloadingCrewCommand request, CancellationToken cancellationToken)
        {
            if (request.CollaboratorIds == null || request.CollaboratorIds.Count == 0)
            {
                return _errorManager.ThrowBadRequest<bool>(
                    "Debe enviar al menos un colaborador (collaborator_ids) para una cuadrilla interna.",
                    "ERP:CREW_MISSING_COLLABORATORS");
            }

            var distinctIds = request.CollaboratorIds.Distinct().ToList();
            var validCount = await _unitOfWork.Collaborators.Entities
                .CountAsync(c => distinctIds.Contains(c.Id) 
                              && c.CompanyId == request.CompanyId 
                              && c.DeletedAt == null 
                              && c.Status == CollaboratorStatus.Active, cancellationToken);

            if (validCount != distinctIds.Count)
            {
                return _errorManager.ThrowBadRequest<bool>(
                    "Uno o mas colaboradores seleccionados no existen o no se encuentran activos.",
                    "ERP:INVALID_COLLABORATORS");
            }

            foreach (var collaboratorId in distinctIds)
            {
                var crew = new CrewAssignments
                {
                    Id = Guid.NewGuid(),
                    WarehouseAssignmentId = assignmentId,
                    AssignedAt = NicaraguaClock.Now,
                    CollaboratorId = collaboratorId,
                    IsOutsourced = false,
                    PersonCount = 1
                };
                await _unitOfWork.CrewAssignments.InsertCrewAssignment(crew);
            }

            return true;
        }

        private async Task<bool> AssignOutsourcedCrewAsync(
            Guid assignmentId, CreateUnloadingCrewCommand request)
        {
            if (!request.PersonCount.HasValue || request.PersonCount.Value <= 0)
            {
                return _errorManager.ThrowBadRequest<bool>(
                    "La cantidad de personas para la cuadrilla tercerizada debe ser mayor a cero.",
                    "ERP:INVALID_PERSON_COUNT");
            }

            if (string.IsNullOrWhiteSpace(request.ProviderName))
            {
                return _errorManager.ThrowBadRequest<bool>(
                    "Debe ingresar el nombre del proveedor para la cuadrilla tercerizada.",
                    "ERP:PROVIDER_NAME_REQUIRED");
            }

            var crew = new CrewAssignments
            {
                Id = Guid.NewGuid(),
                WarehouseAssignmentId = assignmentId,
                AssignedAt = NicaraguaClock.Now,
                CollaboratorId = null,
                IsOutsourced = true,
                PersonCount = request.PersonCount.Value,
                ProviderName = request.ProviderName.Trim(),
                InvoiceNumber = request.InvoiceNumber?.Trim()
            };

            await _unitOfWork.CrewAssignments.InsertCrewAssignment(crew);
            return true;
        }
    }
}
