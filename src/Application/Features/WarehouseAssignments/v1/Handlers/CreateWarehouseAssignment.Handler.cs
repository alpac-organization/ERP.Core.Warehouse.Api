using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Commands;
using ERP.Core.Warehouse.Api.Application.Commons.Constants;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;
using WarehouseAssignmentEntity = ERP.Core.Database.Domain.Entities.Warehouse.WarehouseAssignments;

namespace ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Handlers
{
    public class CreateWarehouseAssignmentHandler : BaseValidatorHandler<CreateWarehouseAssignmentCommand, bool>
    {
        public CreateWarehouseAssignmentHandler(IUnitOfWork unitOfWork, IErrorManager errorManager)
            : base(unitOfWork, errorManager)
        {
        }

        public override async Task<bool> Handle(CreateWarehouseAssignmentCommand request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
            if (!access.IsSuccess) return access.ErrorResponse!;

            var recordEntrance = await _unitOfWork.RecordEntrance.Entities
                .Include(r => r.ReceptionEntrance!)
                .Include(r => r.EntranceDucats)
                .Include(r => r.CustomsDeclarations!)
                    .ThenInclude(cd => cd.Details)
                .FirstOrDefaultAsync(r => r.Id == request.ReceptionId && r.DeletedAt == null, cancellationToken);

            if (recordEntrance == null || recordEntrance.ReceptionEntrance == null)
            {
                return _errorManager.ThrowBadRequest<bool>(
                    "El registro de recepción no fue encontrado o ya ha sido eliminado.",
                    "ERP:RECEPTION_NOT_FOUND");
            }


            if (!WarehouseAssignmentRules.IsStepTwoCompleted(recordEntrance, request.EntranceDucatId))
            {
                return _errorManager.ThrowBadRequest<bool>(
                    "El documento o DUCA especificado aún no ha completado el registro de mercadería.",
                    "ERP:STEP_TWO_NOT_COMPLETED");
            }

            if (request.EntranceDucatId.HasValue)
            {
                var ducaBelongsToReception = recordEntrance.EntranceDucats
                    .Any(d => d.Id == request.EntranceDucatId.Value && d.DeletedAt == null);

                if (!ducaBelongsToReception)
                {
                    return _errorManager.ThrowBadRequest<bool>(
                        "La DUCA especificada no pertenece a esta recepción.",
                        "ERP:DUCA_NOT_FOUND_IN_RECEPTION");
                }
            }


            var alreadyAssigned = await _unitOfWork.WarehouseAssignments.Entities
                .AnyAsync(a => a.RecordEntranceId == request.ReceptionId 
                            && a.EntranceDucatId == request.EntranceDucatId 
                            && a.DeletedAt == null, cancellationToken);

            if (alreadyAssigned)
            {
                return _errorManager.ThrowBadRequest<bool>(
                    "Esta recepción o DUCA ya tiene una bodega asignada.",
                    "ERP:WAREHOUSE_ALREADY_ASSIGNED");
            }

            var warehouse = await _unitOfWork.Warehouses.Entities
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.Id == request.WarehouseId && w.DeletedAt == null, cancellationToken);

            if (warehouse == null || !warehouse.IsActive)
            {
                return _errorManager.ThrowBadRequest<bool>(
                    "La bodega seleccionada no existe o se encuentra inactiva.",
                    "ERP:WAREHOUSE_NOT_FOUND");
            }

            var allowedType = WarehouseAssignmentRules.AllowedWarehouseType(recordEntrance.ReceptionEntrance.DocumentType);
            if (warehouse.WarehouseType != allowedType)
            {
                var typeLabel = allowedType == WarehouseType.Fiscal ? "fiscal" : "general";
                return _errorManager.ThrowBadRequest<bool>(
                    $"Este tipo de documento solo puede asignarse a bodegas de tipo {typeLabel}.",
                    "ERP:WAREHOUSE_TYPE_NOT_ALLOWED");
            }

            var nowNica = NicaraguaClock.Now;
            var today = NicaraguaClock.Today;
            var now = NicaraguaClock.TimeNow;

            var user = await _unitOfWork.Users.Entities
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
            var registeredByUserName = user?.Fullname ?? user?.UserName ?? request.UserId.ToString();

            var assignment = new WarehouseAssignmentEntity
            {
                Id = Guid.NewGuid(),
                RecordEntranceId = request.ReceptionId,
                EntranceDucatId = request.EntranceDucatId,
                WarehouseId = request.WarehouseId,
                WarehouseKeeperUserId = request.WarehouseChiefUserId.ToString(),
                UnloadingStartTime = nowNica,
                AssignedAt = nowNica,
                AssignedByUserId = request.UserId.ToString()
            };

            var executionLog = await _unitOfWork.StepExecutionLogs.Entities
                .FirstOrDefaultAsync(l => l.RecordEntranceId == recordEntrance.Id 
                                       && l.WorkflowStepDefinitionCode == WorkflowStepCodes.Assignment, cancellationToken);

            if (executionLog == null)
            {
                executionLog = new StepExecutionLogs
                {
                    Id = Guid.NewGuid(),
                    RecordEntranceId = recordEntrance.Id,
                    WorkflowStepDefinitionCode = WorkflowStepCodes.Assignment,
                    StartDate = today,
                    StartTime = now,
                    EndDate = null,
                    EndTime = null,
                    ProcessedByUserId = request.UserId.ToString(),
                    ProcessedByUserName = registeredByUserName
                };
                await _unitOfWork.StepExecutionLogs.InsertExecutionLog(executionLog);
            }

            recordEntrance.CurrentStepCode = WorkflowStepCodes.Assignment;

            await _unitOfWork.WarehouseAssignments.InsertWarehouseAssignment(assignment);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
