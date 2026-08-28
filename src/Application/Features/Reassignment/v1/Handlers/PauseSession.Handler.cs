using ERP.Core.Database.Domain.Enums;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Handlers;

public class PauseSessionHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, SessionAccessValidator sessionValidator)
    : BaseValidatorHandler<PauseSessionCommand, bool>(unitOfWork, errorManager)
{
    public override async Task<bool> Handle(PauseSessionCommand request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var session = await sessionValidator.ValidateSession(request.SessionId, request.UserId.ToString(), cancellationToken);

        session.Status = ReassignmentSessionStatus.Paused;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}