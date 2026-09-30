using MediatR;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Entities.Operations;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Commons.Interfaces;

namespace ERP.Core.Warehouse.Api.Application.Commons.Bases;

public abstract class BaseAssignmentOperationalHandler<TRequest, TResponse>(IUnitOfWork unitOfWork, IErrorManager errorManager)
    : BaseValidatorHandler<TRequest, TResponse>(unitOfWork, errorManager)
    where TRequest : IRequest<TResponse>
{
    protected async Task<(bool IsValid, AssignmentOperational? Assignment, Guid BranchId, TResponse ErrorResponse)>
        ValidateAssignmentAccessAsync<TQuery>(TQuery request, Guid assignmentId,
            CancellationToken cancellationToken, bool trackChanges = false)
        where TQuery : IAssignmentOperationalRequest
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);

        if (!access.IsSuccess)
            return (false, null, default, access.ErrorResponse!);

        IQueryable<AssignmentOperational> assignmentQuery = _unitOfWork.AssignmentOperationals.Entities
            .Include(ao => ao.OperationalOrder);

        if (!trackChanges)
            assignmentQuery = assignmentQuery.AsNoTracking();

        var assignmentOperational = await assignmentQuery
            .FirstOrDefaultAsync(ao => ao.Id == assignmentId, cancellationToken);

        if (assignmentOperational is null)
            return (false, null, default, _errorManager.ThrowBadRequest<TResponse>(
                "La asignacion operativa seleccionada no existe", "ERP:ASSIGNMENT_OPERATIONAL_NOT_FOUND"));

        if (assignmentOperational.OperationalOrderId != request.OperationalOrderId)
            return (false, null, default, _errorManager.ThrowBadRequest<TResponse>(
                "La asignacion operativa no pertenece a la orden operativa indicada", "ERP:ASSIGNMENT_OPERATIONAL_ORDER_MISMATCH"));

        if (assignmentOperational.OperationalOrder.CompanyId != request.CompanyId)
            return (false, null, default, _errorManager.ThrowForbidden<TResponse>(
                "No tienes acceso a la asignacion operativa seleccionada", "ERP:ASSIGNMENT_OPERATIONAL_COMPANY_MISMATCH"));

        return (true, assignmentOperational, access.Profile.BranchId, default!);
    }
}
