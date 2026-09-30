using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Handlers;

public class DeleteAssignmentCollaboratorsHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager)
    : BaseValidatorHandler<DeleteAssignmentCollaboratorsCommand, bool>(_unitOfWork, _errorManager)
{
    public override async Task<bool> Handle(DeleteAssignmentCollaboratorsCommand request, CancellationToken ct)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, ct);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var assignmentOperational = await _unitOfWork.AssignmentOperationals.Entities
            .Include(ao => ao.OperationalOrder)
            .FirstOrDefaultAsync(ao => ao.Id == request.AssignmentId, ct);

        if (assignmentOperational is null)
            return _errorManager.ThrowBadRequest<bool>(
                "La asignacion operativa seleccionada no existe", "ERP:ASSIGNMENT_OPERATIONAL_NOT_FOUND");

        if (assignmentOperational.OperationalOrderId != request.OperationalOrderId)
            return _errorManager.ThrowBadRequest<bool>(
                "La asignacion operativa no pertenece a la orden operativa indicada", "ERP:ASSIGNMENT_OPERATIONAL_ORDER_MISMATCH");

        if (assignmentOperational.OperationalOrder.CompanyId != request.CompanyId)
            return _errorManager.ThrowForbidden<bool>(
                "No tienes acceso a la asignacion operativa seleccionada", "ERP:ASSIGNMENT_OPERATIONAL_COMPANY_MISMATCH");

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

        assignmentOperational.HasCollaboratorsAssigned = hasRemainingCollaborators;

        await _unitOfWork.AssignmentCollaborators.UpdateAsync(collaborator);
        await _unitOfWork.AssignmentOperationals.UpdateAsync(assignmentOperational);
        await _unitOfWork.SaveChangesAsync(ct);

        return true;
    }
}
