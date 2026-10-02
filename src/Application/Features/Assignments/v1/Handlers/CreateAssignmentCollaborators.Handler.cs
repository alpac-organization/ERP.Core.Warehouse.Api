using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Handlers;

public class CreateAssignmentCollaboratorsHandler(IUnitOfWork unitOfWork, IErrorManager errorManager,
    ILogger<CreateAssignmentCollaboratorsHandler> logger) : BaseAssignmentOperationalHandler<CreateAssignmentCollaboratorsCommand, Unit>(unitOfWork, errorManager)
{
    public override async Task<Unit> Handle(CreateAssignmentCollaboratorsCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("👷‍♂️​ Iniciando asignamiento de Colaboradores.");

        var (isValid, assignmentOperational, branchId, error) = await ValidateAssignmentAccessAsync(
            request, request.AssignmentOperationalId, cancellationToken, trackChanges: true);

        if (!isValid) return error;

        var collaboratorIds = request.Collaborators.Distinct().ToList();

        var existingIds = await _unitOfWork.Collaborators.Entities
            .AsNoTracking()
            .Where(c => collaboratorIds.Contains(c.Id)
                && c.WorkingInformation != null
                && c.WorkingInformation.BranchId == branchId)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        var missing = collaboratorIds.Except(existingIds).ToList();

        if (missing.Count > 0)
            return _errorManager.ThrowBadRequest<Unit>(
                $"El colaborador indicado no existe o no pertenece a la sucursal: {string.Join(", ", missing)}",
                "ERP:COLLABORATOR_NOT_FOUND");

        var alreadyAssignedIds = await _unitOfWork.AssignmentCollaborators.Entities
            .AsNoTracking()
            .Where(ac => ac.AssignmentOperationalId == request.AssignmentOperationalId
                && collaboratorIds.Contains(ac.CollaboratorId))
            .Select(ac => ac.CollaboratorId)
            .ToListAsync(cancellationToken);

        var duplicates = collaboratorIds.Intersect(alreadyAssignedIds).ToList();

        if (duplicates.Count > 0)
            return _errorManager.ThrowBadRequest<Unit>(
                $"Uno o más colaboradores ya fueron asignados a esta asignacion operativa",
                "ERP:COLLABORATOR_ALREADY_ASSIGNED");

        var entities = request.ToAssignmentCollaboratorsEntities(assignmentOperational!.OperationalOrderId);

        foreach (var entity in entities)
        {
            await _unitOfWork.AssignmentCollaborators.AssignCollaborator(entity);
        }

        assignmentOperational.HasCollaboratorsAssigned = true;

        await _unitOfWork.AssignmentOperationals.UpdateAsync(assignmentOperational);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("👷‍♂️​ Se asignaron {Count} colaborador(es) a la asignacion operativa {AssignmentOperationalId}", entities.Count, request.AssignmentOperationalId);

        return Unit.Value;
    }
}
