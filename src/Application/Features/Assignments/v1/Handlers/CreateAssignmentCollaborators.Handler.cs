using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Handlers;

public class CreateAssignmentCollaboratorsHandler(IUnitOfWork unitOfWork, IErrorManager errorManager,
    ILogger<CreateAssignmentCollaboratorsHandler> logger) : BaseValidatorHandler<CreateAssignmentCollaboratorsCommand, Unit>(unitOfWork, errorManager)
{
    public override async Task<Unit> Handle(CreateAssignmentCollaboratorsCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("🛫 Iniciando asignamiento de Colaboradores.");

        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);

        if (!access.IsSuccess) return access.ErrorResponse!;

        var assignmentOperational = await _unitOfWork.AssignmentOperationals.Entities
            .Include(ao => ao.OperationalOrder)
            .FirstOrDefaultAsync(ao => ao.Id == request.AssignmentOperationalId, cancellationToken);

        if (assignmentOperational is null)
            return _errorManager.ThrowBadRequest<Unit>("La asignacion operativa seleccionada no existe", "ERP:ASSIGNMENT_OPERATIONAL_NOT_FOUND");

        if (assignmentOperational.OperationalOrderId != request.OperationalOrderId)
            return _errorManager.ThrowBadRequest<Unit>("La asignacion operativa no pertenece a la orden operativa indicada", "ERP:ASSIGNMENT_OPERATIONAL_ORDER_MISMATCH");

        if (assignmentOperational.OperationalOrder.CompanyId != request.CompanyId)
            return _errorManager.ThrowForbidden<Unit>("No tienes acceso a la asignacion operativa seleccionada", "ERP:ASSIGNMENT_OPERATIONAL_COMPANY_MISMATCH");

        var entities = request.ToAssignmentCollaboratorsEntities(assignmentOperational.OperationalOrderId);

        foreach (var entity in entities)
        {
            await _unitOfWork.AssignmentCollaborators.AssignCollaborator(entity);
        }

        assignmentOperational.HasCollaboratorsAssigned = true;

        await _unitOfWork.AssignmentOperationals.UpdateAsync(assignmentOperational);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Se asignaron {Count} colaborador(es) a la asignacion operativa {AssignmentOperationalId}", entities.Count, request.AssignmentOperationalId);

        return Unit.Value;
    }
}
