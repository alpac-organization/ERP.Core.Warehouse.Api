using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Handlers;

public class SessionAccessValidator(IUnitOfWork unitOfWork, IErrorManager errorManager)
{
    public async Task<ReassignmentSessions> ValidateSession(
        Guid sessionId,
        string userIdStr,
        CancellationToken ct)
    {
        var session = await unitOfWork.ReassignmentSessions.Entities
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.DeletedAt == null, ct);

        if (session is null)
            return errorManager.ThrowNotFound<ReassignmentSessions>(
                "La sesión de reasignamiento no existe.",
                "ERP:REASSIGNMENT_SESSION_NOT_FOUND");

        if (session.Status != ReassignmentSessionStatus.Open)
            return errorManager.ThrowBadRequest<ReassignmentSessions>(
                "La sesión no está abierta.",
                "ERP:REASSIGNMENT_SESSION_NOT_OPEN");

        if (session.CurrentOwnerUserId != userIdStr)
            return errorManager.ThrowForbidden<ReassignmentSessions>(
                "Solo el dueño actual de la sesión puede operar sobre ella.",
                "ERP:NOT_SESSION_OWNER");

        return session;
    }
}
