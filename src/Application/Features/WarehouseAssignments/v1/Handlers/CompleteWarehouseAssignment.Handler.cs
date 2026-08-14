using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Database.Domain.Enums;
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

        var context = await WarehouseDocumentLookup.FindDocumentAsync(
            _unitOfWork, request.DocumentId, request.DocumentType, cancellationToken);

        if (context == null)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "El documento no fue encontrado o ya ha sido eliminado.",
                "ERP:DOCUMENT_NOT_FOUND");
        }

        var assignment = await _unitOfWork.WarehouseAssignments.Entities
            .Include(a => a.UnloadingDetails)
            .FirstOrDefaultAsync(a => a.DeletedAt == null &&
                (request.DocumentType == DocumentType.DUCA
                    ? a.EntranceDucatId == request.DocumentId
                    : a.CustomsDeclarationId == request.DocumentId), cancellationToken);

        if (assignment == null)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "Este documento no tiene una bodega asignada.",
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
                l.RecordEntranceId == context.RecordEntrance.Id &&
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

        if (assignment.UnloadingDetails != null && assignment.UnloadingDetails.UnloadingEndTime == null)
        {
            assignment.UnloadingDetails.UnloadingEndTime = nowNica;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
