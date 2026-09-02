using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Commands;
using ERP.Core.Warehouse.Api.Application.Commons.Constants;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;

namespace ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Handlers
{
    public class CompleteWarehouseAssignmentHandler : BaseValidatorHandler<CompleteWarehouseAssignmentCommand, bool>
    {
        public CompleteWarehouseAssignmentHandler(IUnitOfWork unitOfWork, IErrorManager errorManager)
            : base(unitOfWork, errorManager)
        {
        }

        public override async Task<bool> Handle(CompleteWarehouseAssignmentCommand request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
            if (!access.IsSuccess) return access.ErrorResponse!;

            var recordEntrance = await _unitOfWork.RecordEntrance.Entities
                .FirstOrDefaultAsync(r => r.Id == request.ReceptionId && r.DeletedAt == null, cancellationToken);

            if (recordEntrance == null)
            {
                return _errorManager.ThrowBadRequest<bool>("El registro de recepción no fue encontrado.", "ERP:RECEPTION_NOT_FOUND");
            }

            var assignmentQuery = _unitOfWork.WarehouseAssignments.Entities
                .Where(a => a.RecordEntranceId == request.ReceptionId && a.DeletedAt == null);

            if (request.EntranceDucatId.HasValue)
            {
                assignmentQuery = assignmentQuery.Where(a => a.EntranceDucatId == request.EntranceDucatId.Value);
            }
            else
            {
                assignmentQuery = assignmentQuery.Where(a => a.EntranceDucatId == null);
            }

            var assignment = await assignmentQuery.FirstOrDefaultAsync(cancellationToken);

            if (assignment == null)
            {
                return _errorManager.ThrowBadRequest<bool>("No se encontró la asignación de bodega para esta recepción/DUCA.", "ERP:ASSIGNMENT_NOT_FOUND");
            }

            var nowNica = NicaraguaClock.Now;
            var today = NicaraguaClock.Today;
            var now = NicaraguaClock.TimeNow;

            if (assignment.UnloadingEndTime == null)
            {
                assignment.UnloadingEndTime = nowNica;
            }

            var executionLog = await _unitOfWork.StepExecutionLogs.Entities
                .FirstOrDefaultAsync(l => l.RecordEntranceId == recordEntrance.Id 
                                       && l.WorkflowStepDefinitionCode == WorkflowStepCodes.Assignment, cancellationToken);

            if (executionLog != null && executionLog.EndDate == null)
            {
                var user = await _unitOfWork.Users.Entities.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
                var registeredByUserName = user?.Fullname ?? user?.UserName ?? request.UserId.ToString();

                executionLog.EndDate = today;
                executionLog.EndTime = now;
                executionLog.FinishedByUserId = request.UserId.ToString();
                executionLog.FinishedByUserName = registeredByUserName;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
