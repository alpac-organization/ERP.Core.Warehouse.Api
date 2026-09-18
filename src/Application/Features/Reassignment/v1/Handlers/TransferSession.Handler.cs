using Microsoft.EntityFrameworkCore;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Handlers;

public class TransferSessionHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, SessionAccessValidator sessionValidator)
    : BaseValidatorHandler<TransferSessionCommand, bool>(unitOfWork, errorManager)
{
    public override async Task<bool> Handle(
        TransferSessionCommand request,
        CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var session = await sessionValidator.ValidateOwnership(request.SessionId, request.UserId.ToString(), cancellationToken);

        EnsureTransferable(session, request.NewOwnerUserId);
        await EnsureHasActivity(request.SessionId, cancellationToken);

        var nowNica = NicaraguaClock.Now;
        var nowDate = DateOnly.FromDateTime(nowNica);
        var nowTime = TimeOnly.FromDateTime(nowNica);

        var activeLog = await FindActiveOwnershipLog(request.SessionId, cancellationToken);
        activeLog.EndedAtDate = nowDate;
        activeLog.EndedAtTime = nowTime;

        var newLog = TransferSessionMapper.ToOwnershipLogEntity(request.SessionId, request.NewOwnerUserId, nowDate, nowTime);
        await _unitOfWork.ReassignmentSessionOwnershipLog.InsertReassignmentSessionOwnershipLog(newLog);

        session.CurrentOwnerUserId = request.NewOwnerUserId;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    private void EnsureTransferable(ReassignmentSessions session, string newOwnerUserId)
    {
        if (session.Status == ReassignmentSessionStatus.Closed)
            _errorManager.ThrowBadRequest<ReassignmentSessions>(
                "La sesión está cerrada; no se puede transferir.",
                "ERP:CANNOT_TRANSFER_CLOSED");

        if (string.Equals(session.CurrentOwnerUserId, newOwnerUserId, StringComparison.OrdinalIgnoreCase))
            _errorManager.ThrowBadRequest<ReassignmentSessions>(
                "No se puede transferir la sesión a su dueño actual.",
                "ERP:SAME_OWNER_TRANSFER");
    }

    private async Task EnsureHasActivity(Guid sessionId, CancellationToken ct)
    {
        var hasActivity = await _unitOfWork.ReassignmentMemoryItems.Entities
            .AnyAsync(m => m.ReassignmentSessionId == sessionId && m.DeletedAt == null, ct);

        if (!hasActivity)
            _errorManager.ThrowBadRequest<ReassignmentSessions>(
                "La sesión no tiene polines levantados; no se puede transferir.",
                "ERP:CANNOT_TRANSFER_EMPTY");
    }

    private async Task<ReassignmentSessionOwnershipLog> FindActiveOwnershipLog(Guid sessionId, CancellationToken ct)
    {
        var activeLog = await _unitOfWork.ReassignmentSessionOwnershipLog.Entities
            .FirstOrDefaultAsync(l => l.ReassignmentSessionId == sessionId
                && l.EndedAtDate == null
                && l.DeletedAt == null, ct);

        if (activeLog is null)
            return _errorManager.ThrowBadRequest<ReassignmentSessionOwnershipLog>(
                "La sesión no tiene una asignación de dueño vigente.",
                "ERP:NO_ACTIVE_OWNERSHIP");

        return activeLog;
    }
}
