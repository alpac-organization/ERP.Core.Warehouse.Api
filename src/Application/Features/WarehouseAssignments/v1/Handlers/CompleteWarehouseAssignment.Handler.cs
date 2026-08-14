using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Handlers;

public class CompleteWarehouseAssignmentHandler(IUnitOfWork unitOfWork, IErrorManager errorManager)
    : BaseValidatorHandler<CompleteWarehouseAssignmentCommand, bool>(unitOfWork, errorManager)
{
    public override async Task<bool> Handle(CompleteWarehouseAssignmentCommand request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var recordEntrance = await _unitOfWork.RecordEntrance.Entities
            .Include(r => r.Assignment)
            .Include(r => r.UnloadingDetails)
            .FirstOrDefaultAsync(r => r.Id == request.ReceptionId && r.DeletedAt == null, cancellationToken);

        if (recordEntrance == null)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "El registro de recepción no fue encontrado o ya ha sido eliminado.",
                "ERP:RECEPTION_NOT_FOUND");
        }

        if (recordEntrance.Assignment == null)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "Esta recepción no tiene una bodega asignada.",
                "ERP:WAREHOUSE_ASSIGNMENT_REQUIRED");
        }

        var user = await _unitOfWork.Users.Entities
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "No se pudo identificar al usuario autenticado en el sistema.",
                "ERP:USER_NOT_FOUND");
        }

        var currentUserName = user.Fullname ?? user.UserName ?? request.UserId.ToString();
        var nowNica = NicaraguaClock.Now;
        var today = DateOnly.FromDateTime(nowNica);
        var now = TimeOnly.FromDateTime(nowNica);

        var executionLog = await _unitOfWork.StepExecutionLogs.Entities
            .FirstOrDefaultAsync(l =>
                l.RecordEntranceId == recordEntrance.Id &&
                l.WorkflowStepDefinitionCode == WarehouseAssignmentRules.AssignmentStepCode,
                cancellationToken);

        if (executionLog == null)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "No se encontró el registro de ejecución del paso de asignación de bodega.",
                "ERP:ASSIGNMENT_LOG_NOT_FOUND");
        }

        if (executionLog.EndDate == null)
        {
            executionLog.EndDate = today;
            executionLog.EndTime = now;
            executionLog.FinishedByUserId = request.UserId.ToString();
            executionLog.FinishedByUserName = currentUserName;
        }

        if (recordEntrance.UnloadingDetails != null && recordEntrance.UnloadingDetails.UnloadingEndTime == null)
        {
            recordEntrance.UnloadingDetails.UnloadingEndTime = nowNica;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}