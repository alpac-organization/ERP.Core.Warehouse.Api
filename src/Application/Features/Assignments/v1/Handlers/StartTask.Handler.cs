using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Handlers;

public class StartTaskHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager)
    : BaseValidatorHandler<StartTaskCommand, Unit>(_unitOfWork, _errorManager)
{
    public override async Task<Unit> Handle(StartTaskCommand request, CancellationToken ct)
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

        if (assignment.Status == AssignmentOperationalStatus.InProgress)
            return _errorManager.ThrowBadRequest<Unit>("La operación ya está en proceso/ejecución.",
                "ERP:ASSIGNMENT_IN_PROGRESS");

        if (assignment.Status != AssignmentOperationalStatus.Pending)
            return _errorManager.ThrowBadRequest<Unit>("La asignación no está pendiente para iniciar la tarea.",
                "ERP:ASSIGNMENT_NOT_PENDING");

        assignment.Status = AssignmentOperationalStatus.InProgress;

        await _unitOfWork.AssignmentOperationals.UpdateAsync(assignment);
        await _unitOfWork.SaveChangesAsync(ct);

        return Unit.Value;
    }
}