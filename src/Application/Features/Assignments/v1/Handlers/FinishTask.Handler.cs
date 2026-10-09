using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Handlers;

public class FinishTaskHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager)
    : BaseValidatorHandler<FinishTaskCommand, Unit>(_unitOfWork, _errorManager)
{
    public override async Task<Unit> Handle(FinishTaskCommand request, CancellationToken ct)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, ct);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var assignment = await AssignmentOperationalFinder.FindActiveWithStockPlacementsAsync(
            _unitOfWork, request.AssignmentId, request.OperationalOrderId, ct);

        if (assignment is null)
            return _errorManager.ThrowNotFound<Unit>("No se encontró la asignación solicitada.",
                "ERP:ASSIGNMENT_NOT_FOUND");

        if (assignment.Status == AssignmentOperationalStatus.Downloaded)
            return _errorManager.ThrowBadRequest<Unit>("La operación ya fue finalizada.",
                "ERP:ASSIGNMENT_DOWNLOADED");

        if (assignment.Status != AssignmentOperationalStatus.InProgress)
            return _errorManager.ThrowBadRequest<Unit>("La asignación no está en proceso.",
                "ERP:ASSIGNMENT_NOT_IN_PROGRESS");

        if (!assignment.HasPositionatingInformation)
            return _errorManager.ThrowBadRequest<Unit>(
                "Debe registrar la información de polines antes de finalizar la tarea.",
                "ERP:ASSIGNMENT_NO_POSITIONATING_INFORMATION");

        var stockPlacements = assignment.AssignmentStockPlacements.ToList();

        if (stockPlacements.Count == 0)
            return _errorManager.ThrowBadRequest<Unit>(
                "Debe asignar al menos una posición de almacén antes de finalizar la tarea.",
                "ERP:ASSIGNMENT_NO_POSITIONS");

        foreach (var stockPlacement in stockPlacements)
        {
            if (stockPlacement.LotPosition?.Status == RackStatus.Reserved)
            {
                stockPlacement.LotPosition.Status = RackStatus.Occupied;
                await _unitOfWork.LotsPositions.UpdateAsync(stockPlacement.LotPosition);
            }

            if (stockPlacement.RackPosition?.Status == RackStatus.Reserved)
            {
                stockPlacement.RackPosition.Status = RackStatus.Occupied;
                await _unitOfWork.RackPositions.UpdateAsync(stockPlacement.RackPosition);
            }
        }

assignment.Status = AssignmentOperationalStatus.Downloaded;

        var hasNonDownloadedAssignments = await _unitOfWork.AssignmentOperationals.Entities
            .AnyAsync(a => a.OperationalOrderId == request.OperationalOrderId
                && a.Id != assignment.Id
                && a.IsActive
                && a.DeletedAt == null
                && a.Status != AssignmentOperationalStatus.Downloaded, ct);

        if (!hasNonDownloadedAssignments)
        {
            assignment.OperationalOrder.Status = OperationalOrderStatus.Completed;
            await _unitOfWork.OperationalOrders.UpdateAsync(assignment.OperationalOrder);
        }

        await _unitOfWork.AssignmentOperationals.UpdateAsync(assignment);
        await _unitOfWork.SaveChangesAsync(ct);

        return Unit.Value;
    }
}