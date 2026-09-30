using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Handlers;

public class DeleteAssignmentMachineryHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager)
    : BaseAssignmentOperationalHandler<DeleteAssignmentMachineryCommand, bool>(_unitOfWork, _errorManager)
{
    public override async Task<bool> Handle(DeleteAssignmentMachineryCommand request, CancellationToken ct)
    {
        var (isValid, assignmentOperational, _, error) = await ValidateAssignmentAccessAsync(
            request, request.AssignmentId, ct, trackChanges: true);

        if (!isValid) return error;

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

        assignmentOperational!.HasMachineryAssigned = hasRemainingMachinery;

        await _unitOfWork.AssignmentsMachineries.UpdateAsync(machinery);
        await _unitOfWork.AssignmentOperationals.UpdateAsync(assignmentOperational);
        await _unitOfWork.SaveChangesAsync(ct);

        return true;
    }
}
