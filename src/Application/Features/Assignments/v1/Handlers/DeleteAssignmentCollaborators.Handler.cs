using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Handlers;

public class DeleteAssignmentCollaboratorsHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager) : BaseAssignmentOperationalHandler<DeleteAssignmentCollaboratorsCommand, bool>(_unitOfWork, _errorManager)
{
    public override async Task<bool> Handle(DeleteAssignmentCollaboratorsCommand request, CancellationToken ct)
    {
        var (isValid, assignmentOperational, _, error) = await ValidateAssignmentAccessAsync(
            request, request.AssignmentId, ct, trackChanges: true);

        if (!isValid) return error;

        var collaborator = await _unitOfWork.AssignmentCollaborators.Entities
            .Where(ac => ac.Id == request.AssignmentCollaboratorId
                && ac.AssignmentOperationalId == request.AssignmentId
                && ac.IsActive
                && ac.DeletedAt == null)
            .FirstOrDefaultAsync(ct);

        if (collaborator is null)
            return _errorManager.ThrowBadRequest<bool>(
                "El colaborador asignado no existe", "ERP:ASSIGNMENT_COLLABORATOR_NOT_FOUND");

        var hasRemainingCollaborators = await _unitOfWork.AssignmentCollaborators.Entities
            .AnyAsync(ac => ac.AssignmentOperationalId == request.AssignmentId
                && ac.Id != collaborator.Id
                && ac.IsActive
                && ac.DeletedAt == null, ct);

        collaborator.IsActive = false;
        collaborator.DeletedAt = DateTime.UtcNow;

        assignmentOperational!.HasCollaboratorsAssigned = hasRemainingCollaborators;

        await _unitOfWork.AssignmentCollaborators.UpdateAsync(collaborator);
        await _unitOfWork.AssignmentOperationals.UpdateAsync(assignmentOperational);
        await _unitOfWork.SaveChangesAsync(ct);

        return true;
    }
}
