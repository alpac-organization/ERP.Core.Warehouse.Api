using MediatR;
using AutoMapper;

using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Operations;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Handlers
{
    public class UpdateAssignmentHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, IMapper _mapper) : BaseValidatorHandler<UpdateAssignmentCommand, Unit>(_unitOfWork, _errorManager)
    {
        public override async Task<Unit> Handle(UpdateAssignmentCommand request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            if (access.Role?.RoleType == RoleType.Supervisor)
            {
                return _errorManager.ThrowUnauthorized<Unit>("No tienes acceso a realizar esta acción", "ERP:INVALID_ACCESS");
            }

            var assignment = await _unitOfWork.AssignmentOperationals.Entities
                .Where(assignment => assignment.Id == request.AssignmentId)
                .Where(assignment => assignment.OperationalOrderId == request.OperationalOrderId)
                .FirstOrDefaultAsync(cancellationToken);


            if (assignment == null)
            {
                return _errorManager.ThrowNotFound<Unit>("La asignación no existe", "ERP:NOT_FOUND_ASSIGNMENT");
            }

            if (request.Status.HasValue)
            {
                assignment.Status = request.Status.Value;
            }

            if (request.Enclosure != null)
            {
                if (assignment.AssignmentEnclosure == null)
                {
                    assignment.AssignmentEnclosure = new AssignmentEnclosure
                    {
                        Id = Guid.NewGuid(),
                        AssignmentOperationalId = assignment.Id
                    };
                    await _unitOfWork.AssignmentEnclosures.AddAsync(assignment.AssignmentEnclosure);
                    assignment.HasEnclosureAssigned = true;
                }

                assignment.AssignmentEnclosure.Observations = request.Enclosure.Observations;
                assignment.AssignmentEnclosure.Merchandise = request.Enclosure.Merchandise;
                assignment.AssignmentEnclosure.MerchandiseDescription = request.Enclosure.MerchandiseDescription;
                assignment.AssignmentEnclosure.DestinationType = request.Enclosure.DestinationType;
                assignment.AssignmentEnclosure.WarehouseId = request.Enclosure.WarehouseId;
                
                await _unitOfWork.AssignmentEnclosures.UpdateAsync(assignment.AssignmentEnclosure);
            }

            if (request.Machineries.Count > 0)
            {
                var existingMachineryIds = assignment.AssignmentsMachineries
                    .Where(m => m.IsActive)
                    .Select(m => m.MachineryId)
                    .ToHashSet();

                var newMachineryIds = request.Machineries.Select(m => m.MachineryId).ToHashSet();

                foreach (var machinery in assignment.AssignmentsMachineries.Where(m => m.IsActive && !newMachineryIds.Contains(m.MachineryId)))
                {
                    machinery.IsActive = false;
                    await _unitOfWork.AssignmentsMachineries.UpdateAsync(machinery);
                }

                foreach (var machineryData in request.Machineries.Where(m => !existingMachineryIds.Contains(m.MachineryId)))
                {
                    var newMachinery = new AssignmentsMachinery
                    {
                        Id = Guid.NewGuid(),
                        AssignmentOperationalId = assignment.Id,
                        MachineryId = machineryData.MachineryId,
                        Concept = machineryData.Concept,
                        IsActive = true,
                        CreatedByUserId = (await _unitOfWork.Profiles.FirstOrDefaultAsync(p => p.UserId == access.User.Id, cancellationToken))?.Id ?? Guid.Empty
                    };
                    await _unitOfWork.AssignmentsMachineries.AddAsync(newMachinery);
                }

                assignment.HasMachineryAssigned = newMachineryIds.Count > 0;
            }

            if (request.Collaborators.Count > 0)
            {
                var existingCollaboratorIds = assignment.AssignmentCollaborators
                    .Where(c => c.IsActive)
                    .Select(c => c.CollaboratorId)
                    .ToHashSet();

                var newCollaboratorIds = request.Collaborators.Select(c => c.CollaboratorId).ToHashSet();

                foreach (var collaborator in assignment.AssignmentCollaborators.Where(c => c.IsActive && !newCollaboratorIds.Contains(c.CollaboratorId)))
                {
                    collaborator.IsActive = false;
                    await _unitOfWork.AssignmentCollaborators.UpdateAsync(collaborator);
                }

                foreach (var collaboratorData in request.Collaborators.Where(c => !existingCollaboratorIds.Contains(c.CollaboratorId)))
                {
                    var newCollaborator = new AssignmentCollaborators
                    {
                        Id = Guid.NewGuid(),
                        AssignmentOperationalId = assignment.Id,
                        CollaboratorId = collaboratorData.CollaboratorId,
                        Role = collaboratorData.Role,
                        IsActive = true,
                        CreatedByUserId = (await _unitOfWork.Profiles.FirstOrDefaultAsync(p => p.UserId == access.User.Id, cancellationToken))?.Id ?? Guid.Empty,
                        OperationalOrderId = assignment.OperationalOrderId
                    };
                    await _unitOfWork.AssignmentCollaborators.AddAsync(newCollaborator);
                }

                assignment.HasCollaboratorsAssigned = newCollaboratorIds.Count > 0;
            }

            await _unitOfWork.AssignmentOperationals.UpdateAsync(assignment);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}