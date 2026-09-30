using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Handlers;

public class CreateAssignmentMachineryHandler(IUnitOfWork unitOfWork, IErrorManager errorManager,
    ILogger<CreateAssignmentMachineryHandler> logger) : BaseAssignmentOperationalHandler<CreateAssignmentMachineryCommand, Unit>(unitOfWork, errorManager)
{
    public override async Task<Unit> Handle(CreateAssignmentMachineryCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("🚜​ Iniciando asignamiento de Maquinaria.");

        var (isValid, assignmentOperational, _, error) = await ValidateAssignmentAccessAsync(
            request, request.AssignmentOperationalId, cancellationToken, trackChanges: true);

        if (!isValid) return error;

        var machineryIds = request.Machinery.Distinct().ToList();

        var existingIds = await _unitOfWork.Machineries.Entities
            .AsNoTracking()
            .Where(m => machineryIds.Contains(m.Id) && m.IsActive && m.DeletedAt == null)
            .Select(m => m.Id)
            .ToListAsync(cancellationToken);

        var missing = machineryIds.Except(existingIds).ToList();

        if (missing.Count > 0)
            return _errorManager.ThrowBadRequest<Unit>(
                $"La maquinaria indicada no existe o no está activa: {string.Join(", ", missing)}",
                "ERP:MACHINERY_NOT_FOUND");

        var entities = request.ToAssignmentsMachineryEntities();

        foreach (var entity in entities)
        {
            await _unitOfWork.AssignmentsMachineries.AssignMachinery(entity);
        }

        assignmentOperational!.HasMachineryAssigned = true;

        await _unitOfWork.AssignmentOperationals.UpdateAsync(assignmentOperational);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("🚜​ Se asignaron {Count} maquinaria(s)", entities.Count);

        return Unit.Value;
    }
}
