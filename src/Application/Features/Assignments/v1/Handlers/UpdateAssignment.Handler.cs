using MediatR;

using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Handlers
{
    public class UpdateAssignmentHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager) : BaseValidatorHandler<UpdateAssignmentCommand, Unit>(_unitOfWork, _errorManager)
    {
        public override async Task<Unit> Handle(UpdateAssignmentCommand request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            if (access.Role?.RoleType == RoleType.Supervisor)
            {
                return _errorManager.ThrowUnauthorized<Unit>("No tienes acceso a realizar esta acción", "ERP:INVALID_ACCESS");
            }

            var assignment = await _unitOfWork.AssignmentOperationals.Entities
                .Where(assignment => assignment.Id == request.AssignmentId)
                .Where(assignment => assignment.OperationalOrderId == request.OperationalOrderId)
                .FirstOrDefaultAsync(cancellationToken);


            if (assignment == null)
            {
                return _errorManager.ThrowNotFound<Unit>("La asignación no existe", "ERP:NOT_FOUND_ASSIGNMENT");
            }


            await _unitOfWork.AssignmentOperationals.UpdateAsync(assignment);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}