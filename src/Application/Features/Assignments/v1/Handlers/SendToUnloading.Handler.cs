using MediatR;
using Microsoft.Extensions.Logging;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands;
using Microsoft.EntityFrameworkCore;

namespace ERp.Core.Warehouse.Api.Application.Features.Assignments.v1.Handlers;

public class SendToUnloadingHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, ILogger<SendToUnloadingHandler> _logger)
    : BaseValidatorHandler<SendToUnloadingCommand, Unit>(_unitOfWork, _errorManager)
{
    public override async Task<Unit> Handle(SendToUnloadingCommand request, CancellationToken ct)
    {

        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, ct);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var assignment = await _unitOfWork.AssignmentOperationals.Entities
            .Where(a => a.Id == request.AssignmentId && a.OperationalOrderId == request.OperationalOrderId)
            .Where(a => a.IsActive && a.DeletedAt == null)
            .FirstOrDefaultAsync(ct);

        if (assignment is null)
            return _errorManager.ThrowNotFound<Unit>("No se encontró la asignación solicitada.",
                "ERP:ASSIGNMENT_NOT_FOUND");

        if (!assignment.HasCollaboratorsAssigned)
            return _errorManager.ThrowBadRequest<Unit>("No es posible continuar: la asignación no tiene colaboradores asignados.",
            "ERP:ASSIGNMENT_HAS_NO_COLLABORATORS");

        if (!assignment.HasMachineryAssigned)
            return _errorManager.ThrowBadRequest<Unit>("No es posible continuar: la asignación no tiene maquinaria asignada.",
            "ERP:ASSIGNMENT_HAS_NO_MACHINERY");

        if (string.IsNullOrEmpty(assignment.Observations))
            return _errorManager.ThrowBadRequest<Unit>("No es posible continuar: la asignación no tiene instrucciones en el campo \"Observaciones\".",
            "ERP:ASSIGNMENT_HAS_NO_OBSERVATIONS");

        _logger.LogInformation("📤​ Enviando a Bodega...");

        assignment.Status = AssignmentOperationalStatus.OnHold;

        await _unitOfWork.AssignmentOperationals.UpdateAsync(assignment);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("📬 Se ha enviado a Bodega​");

        return Unit.Value;
    }
}