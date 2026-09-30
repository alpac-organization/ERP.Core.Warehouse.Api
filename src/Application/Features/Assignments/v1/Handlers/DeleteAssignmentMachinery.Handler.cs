using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Handlers;

public class DeleteAssignmentMachineryHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager)
    : BaseValidatorHandler<DeleteAssignmentMachineryCommand, bool>(_unitOfWork, _errorManager)
{
    public override async Task<bool> Handle(DeleteAssignmentMachineryCommand request, CancellationToken ct)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, ct);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var assignmentOperational = await _unitOfWork.AssignmentOperationals.Entities
            .Include(ao => ao.OperationalOrder)
            .FirstOrDefaultAsync(ao => ao.Id == request.AssignmentId, ct);

        if (assignmentOperational is null)
            return _errorManager.ThrowBadRequest<bool>(
                "La asignacion operativa seleccionada no existe", "ERP:ASSIGNMENT_OPERATIONAL_NOT_FOUND");

        if (assignmentOperational.OperationalOrderId != request.OperationalOrderId)
            return _errorManager.ThrowBadRequest<bool>(
                "La asignacion operativa no pertenece a la orden operativa indicada", "ERP:ASSIGNMENT_OPERATIONAL_ORDER_MISMATCH");

        if (assignmentOperational.OperationalOrder.CompanyId != request.CompanyId)
            return _errorManager.ThrowForbidden<bool>(
                "No tienes acceso a la asignacion operativa seleccionada", "ERP:ASSIGNMENT_OPERATIONAL_COMPANY_MISMATCH");

        var machinery = await _unitOfWork.AssignmentsMachineries.Entities
            .Where(am => am.Id == request.AssignmentMachineryId
                && am.AssignmentOperationalId == request.AssignmentId
                && am.IsActive
                && am.DeletedAt == null)
            .FirstOrDefaultAsync(ct);

        if (machinery is null)
            return _errorManager.ThrowBadRequest<bool>(
                "La maquinaria asignada no existe", "ERP:ASSIGNMENT_MACHINERY_NOT_FOUND");

        var hasRemainingMachinery = await _unitOfWork.AssignmentsMachineries.Entities
            .AnyAsync(am => am.AssignmentOperationalId == request.AssignmentId
                && am.Id != machinery.Id
                && am.IsActive
                && am.DeletedAt == null, ct);

        machinery.IsActive = false;
        machinery.DeletedAt = DateTime.UtcNow;

        assignmentOperational.HasMachineryAssigned = hasRemainingMachinery;

        await _unitOfWork.AssignmentsMachineries.UpdateAsync(machinery);
        await _unitOfWork.AssignmentOperationals.UpdateAsync(assignmentOperational);
        await _unitOfWork.SaveChangesAsync(ct);

        return true;
    }
}
