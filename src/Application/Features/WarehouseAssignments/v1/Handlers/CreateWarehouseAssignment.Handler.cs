using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
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
        private readonly IMapper _mapper;

        public CreateWarehouseAssignmentHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
            : base(unitOfWork, errorManager)
        {
            _mapper = mapper;
        }

        public override async Task<bool> Handle(CreateWarehouseAssignmentCommand request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
            if (!access.IsSuccess) return access.ErrorResponse!;

            var recordEntrance = await GetRecordEntranceAsync(request.ReceptionId, cancellationToken);
            if (recordEntrance == null || recordEntrance.ReceptionEntrance == null)
            {
                return _errorManager.ThrowBadRequest<bool>(
                    "El registro de recepcion no fue encontrado o ya ha sido eliminado.",
                    "ERP:RECEPTION_NOT_FOUND");
            }

            var prereqValid = await ValidatePrerequisitesAsync(recordEntrance, request, cancellationToken);
            if (!prereqValid) return false;

            var warehouseValid = await ValidateWarehouseAsync(recordEntrance.ReceptionEntrance.DocumentType, request.WarehouseId, cancellationToken);
            if (!warehouseValid) return false;

            await EnsureStepExecutionLogAsync(recordEntrance, request.UserId, cancellationToken);

            var assignment = _mapper.Map<WarehouseAssignmentEntity>(request);
            recordEntrance.CurrentStepCode = WorkflowStepCodes.Assignment;

            await _unitOfWork.WarehouseAssignments.InsertWarehouseAssignment(assignment);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }

        private async Task<RecordEntrance?> GetRecordEntranceAsync(Guid receptionId, CancellationToken cancellationToken)
        {
            return await _unitOfWork.RecordEntrance.Entities
                .AsSplitQuery()
                .Include(r => r.ReceptionEntrance!)
                .Include(r => r.EntranceDucats)
                .Include(r => r.CustomsDeclarations!)
                    .ThenInclude(cd => cd.Details)
                .FirstOrDefaultAsync(r => r.Id == receptionId && r.DeletedAt == null, cancellationToken);
        }

        private async Task<bool> ValidatePrerequisitesAsync(
            RecordEntrance recordEntrance, CreateWarehouseAssignmentCommand request, CancellationToken cancellationToken)
        {
            if (!WarehouseAssignmentRules.IsStepTwoCompleted(recordEntrance, request.EntranceDucatId))
            {
                return _errorManager.ThrowBadRequest<bool>(
                    "El documento o DUCA especificado aun no ha completado el registro de mercaderia.",
                    "ERP:STEP_TWO_NOT_COMPLETED");
            }

            if (request.EntranceDucatId.HasValue)
            {
                var ducaBelongsToReception = recordEntrance.EntranceDucats
                    .Any(d => d.Id == request.EntranceDucatId.Value && d.DeletedAt == null);

                if (!ducaBelongsToReception)
                {
                    return _errorManager.ThrowBadRequest<bool>(
                        "La DUCA especificada no pertenece a esta recepcion.",
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
                    "Esta recepcion o DUCA ya tiene una bodega asignada.",
                    "ERP:WAREHOUSE_ALREADY_ASSIGNED");
            }

            return true;
        }

        private async Task<bool> ValidateWarehouseAsync(
            DocumentType documentType, Guid warehouseId, CancellationToken cancellationToken)
        {
            var warehouse = await _unitOfWork.Warehouses.Entities
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.Id == warehouseId && w.DeletedAt == null, cancellationToken);

            if (warehouse == null || !warehouse.IsActive)
            {
                return _errorManager.ThrowBadRequest<bool>(
                    "La bodega seleccionada no existe o se encuentra inactiva.",
                    "ERP:WAREHOUSE_NOT_FOUND");
            }

            var allowedType = WarehouseAssignmentRules.AllowedWarehouseType(documentType);
            if (warehouse.WarehouseType != allowedType)
            {
                var typeLabel = allowedType == WarehouseType.Fiscal ? "fiscal" : "general";
                return _errorManager.ThrowBadRequest<bool>(
                    $"Este tipo de documento solo puede asignarse a bodegas de tipo {typeLabel}.",
                    "ERP:WAREHOUSE_TYPE_NOT_ALLOWED");
            }

            return true;
        }

        private async Task EnsureStepExecutionLogAsync(
            RecordEntrance recordEntrance, Guid userId, CancellationToken cancellationToken)
        {
            var executionLog = await _unitOfWork.StepExecutionLogs.Entities
                .FirstOrDefaultAsync(l => l.RecordEntranceId == recordEntrance.Id 
                                       && l.WorkflowStepDefinitionCode == WorkflowStepCodes.Assignment, cancellationToken);

            if (executionLog != null) return;

            var user = await _unitOfWork.Users.Entities
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            var registeredByUserName = user?.Fullname ?? user?.UserName ?? userId.ToString();

            executionLog = new StepExecutionLogs
            {
                Id = Guid.NewGuid(),
                RecordEntranceId = recordEntrance.Id,
                WorkflowStepDefinitionCode = WorkflowStepCodes.Assignment,
                StartDate = NicaraguaClock.Today,
                StartTime = NicaraguaClock.TimeNow,
                EndDate = null,
                EndTime = null,
                ProcessedByUserId = userId.ToString(),
                ProcessedByUserName = registeredByUserName
            };

            await _unitOfWork.StepExecutionLogs.InsertExecutionLog(executionLog);
        }
    }
}
