using ERP.Core.Database.Domain.Enums;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Handlers;

public class ResumeSessionHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, SessionAccessValidator sessionValidator)
    : BaseValidatorHandler<ResumeSessionCommand, bool>(unitOfWork, errorManager)
{
    public override async Task<bool> Handle(ResumeSessionCommand request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var session = await sessionValidator.ValidateOwnership(request.SessionId, request.UserId.ToString(), cancellationToken);

        if (session.Status != ReassignmentSessionStatus.Paused)
            _errorManager.ThrowBadRequest<ReassignmentSessions>(
                "La sesión no está pausada; no se puede reanudar.",
                "ERP:REASSIGNMENT_SESSION_NOT_PAUSED");

        session.Status = ReassignmentSessionStatus.Open;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
