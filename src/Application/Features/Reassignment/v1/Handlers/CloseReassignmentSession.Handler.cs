using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Handlers;

public class CloseReassignmentSessionHandler(IUnitOfWork unitOfWork, IErrorManager errorManager)
    : BaseValidatorHandler<CloseReassignmentSessionCommand, bool>(unitOfWork, errorManager)
{
    public override async Task<bool> Handle(
        CloseReassignmentSessionCommand request,
        CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var session = await ValidateSession(request.SessionId, request.UserId.ToString(), cancellationToken);

        await EnsureNoPendingItems(request.SessionId, cancellationToken);

        var nowNica = NicaraguaClock.Now;
        await CloseSession(session, nowNica);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task<ReassignmentSessions> ValidateSession(Guid sessionId, string userIdStr, CancellationToken ct)
    {
        var session = await _unitOfWork.ReassignmentSessions.Entities
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.DeletedAt == null, ct);

        if (session is null)
            return _errorManager.ThrowNotFound<ReassignmentSessions>(
                "La sesión de reasignamiento no existe.",
                "ERP:REASSIGNMENT_SESSION_NOT_FOUND");

        if (session.Status != ReassignmentSessionStatus.Open)
            return _errorManager.ThrowBadRequest<ReassignmentSessions>(
                "La sesión no está abierta; no se puede cerrar.",
                "ERP:REASSIGNMENT_SESSION_NOT_OPEN");

        if (session.CurrentOwnerUserId != userIdStr)
            return _errorManager.ThrowForbidden<ReassignmentSessions>(
                "Solo el dueño actual de la sesión puede operar sobre ella.",
                "ERP:NOT_SESSION_OWNER");

        return session;
    }

    private async Task EnsureNoPendingItems(Guid sessionId, CancellationToken ct)
    {
        var pendingCount = await _unitOfWork.ReassignmentMemoryItems.Entities
            .CountAsync(m => m.ReassignmentSessionId == sessionId
                && m.ResolvedAtDate == null
                && m.DeletedAt == null, ct);

        if (pendingCount > 0)
            _errorManager.ThrowBadRequest<ReassignmentSessions>(
                "Hay polines sin confirmar; no se puede cerrar la sesión.",
                "ERP:PENDING_MEMORY_ITEMS");
    }

    private static Task CloseSession(ReassignmentSessions session, DateTime nowNica)
    {
        session.Status = ReassignmentSessionStatus.Closed;
        session.ClosedAtDate = DateOnly.FromDateTime(nowNica);
        session.ClosedAtTime = TimeOnly.FromDateTime(nowNica);
        return Task.CompletedTask;
    }
}
