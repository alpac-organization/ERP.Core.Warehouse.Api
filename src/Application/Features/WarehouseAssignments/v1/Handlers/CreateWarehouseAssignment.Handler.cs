using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Commands;
using WarehouseAssignmentEntity = ERP.Core.Database.Domain.Entities.Warehouse.WarehouseAssignments;

namespace ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Handlers;

public class CreateWarehouseAssignmentHandler(IUnitOfWork unitOfWork, IErrorManager errorManager)
    : BaseValidatorHandler<CreateWarehouseAssignmentCommand, bool>(unitOfWork, errorManager)
{
    public override async Task<bool> Handle(CreateWarehouseAssignmentCommand request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var recordEntrance = await _unitOfWork.RecordEntrance.Entities
            .Include(r => r.ReceptionEntrance!)
            .Include(r => r.EntranceDucats)
            .Include(r => r.CustomsDeclarations!)
                .ThenInclude(cd => cd.Details)
            .Include(r => r.Assignment)
            .FirstOrDefaultAsync(r => r.Id == request.ReceptionId && r.DeletedAt == null, cancellationToken);

        if (recordEntrance == null || recordEntrance.ReceptionEntrance == null)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "El registro de recepción no fue encontrado o ya ha sido eliminado.",
                "ERP:RECEPTION_NOT_FOUND");
        }

        if (recordEntrance.Assignment != null)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "Esta recepción ya tiene una bodega asignada.",
                "ERP:WAREHOUSE_ALREADY_ASSIGNED");
        }

        if (!WarehouseAssignmentRules.IsStepTwoCompleted(recordEntrance))
        {
            return _errorManager.ThrowBadRequest<bool>(
                "El documento aún no está completado en el paso de registro de mercadería.",
                "ERP:STEP_TWO_NOT_COMPLETED");
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

        Sections? section = null;
        if (request.SectionId.HasValue)
        {
            section = await _unitOfWork.Sections.Entities
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == request.SectionId.Value && s.DeletedAt == null, cancellationToken);

            if (section == null || !section.IsActive)
            {
                return _errorManager.ThrowBadRequest<bool>(
                    "La sección seleccionada no existe o se encuentra inactiva.",
                    "ERP:SECTION_NOT_FOUND");
            }

            if (section.WarehouseId != warehouse.Id)
            {
                return _errorManager.ThrowBadRequest<bool>(
                    "La sección seleccionada no pertenece a la bodega elegida.",
                    "ERP:SECTION_NOT_IN_WAREHOUSE");
            }

            if (section.StorageType == SectionStorageType.Empty)
            {
                return _errorManager.ThrowBadRequest<bool>(
                    "La sección seleccionada no tiene capacidad de almacenamiento.",
                    "ERP:SECTION_EMPTY_STORAGE");
            }
        }

        var rack = await _unitOfWork.Racks.Entities
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.RackId && r.DeletedAt == null, cancellationToken);

        if (rack == null)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "El rack seleccionado no existe.",
                "ERP:RACK_NOT_FOUND");
        }

        if (rack.Status != RackStatus.Available)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "El rack seleccionado no está disponible para asignación.",
                "ERP:RACK_NOT_AVAILABLE");
        }

        var effectiveSectionId = section?.Id ?? rack.SectionId;

        if (section != null && rack.SectionId != section.Id)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "El rack seleccionado no pertenece a la sección elegida.",
                "ERP:RACK_NOT_IN_SECTION");
        }

        Lots? lot = null;
        if (section != null && section.StorageType == SectionStorageType.Lots)
        {
            if (!request.LotsId.HasValue)
            {
                return _errorManager.ThrowBadRequest<bool>(
                    "Debe seleccionar un lote para esta sección.",
                    "ERP:LOT_REQUIRED");
            }

            lot = await _unitOfWork.Lots.Entities
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == request.LotsId.Value && l.DeletedAt == null, cancellationToken);

            if (lot == null)
            {
                return _errorManager.ThrowBadRequest<bool>(
                    "El lote seleccionado no existe.",
                    "ERP:LOT_NOT_FOUND");
            }

            if (lot.SectionId != section.Id)
            {
                return _errorManager.ThrowBadRequest<bool>(
                    "El lote seleccionado no pertenece a la sección elegida.",
                    "ERP:LOT_NOT_IN_SECTION");
            }

            if (lot.Status != RackStatus.Available)
            {
                return _errorManager.ThrowBadRequest<bool>(
                    "El lote seleccionado no está disponible para asignación.",
                    "ERP:LOT_NOT_AVAILABLE");
            }
        }

        if (request.RackPositionsId.HasValue)
        {
            var rackPosition = await _unitOfWork.RackPositions.Entities
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == request.RackPositionsId.Value && p.DeletedAt == null, cancellationToken);

            if (rackPosition == null)
            {
                return _errorManager.ThrowBadRequest<bool>(
                    "La posición del rack seleccionada no existe.",
                    "ERP:RACK_POSITION_NOT_FOUND");
            }

            if (rackPosition.RackId != rack.Id)
            {
                return _errorManager.ThrowBadRequest<bool>(
                    "La posición seleccionada no pertenece al rack elegido.",
                    "ERP:RACK_POSITION_NOT_IN_RACK");
            }

            if (rackPosition.IsBlocked)
            {
                return _errorManager.ThrowBadRequest<bool>(
                    "La posición seleccionada se encuentra bloqueada.",
                    "ERP:RACK_POSITION_BLOCKED");
            }

            var positionInUse = await _unitOfWork.WarehouseAssignments.Entities
                .AnyAsync(a => a.DeletedAt == null && a.RackPositionsId == rackPosition.Id, cancellationToken);

            if (positionInUse)
            {
                return _errorManager.ThrowBadRequest<bool>(
                    "La posición seleccionada ya está ocupada.",
                    "ERP:RACK_POSITION_IN_USE");
            }
        }

        if (request.LotsPositionsId.HasValue)
        {
            if (lot == null)
            {
                return _errorManager.ThrowBadRequest<bool>(
                    "Debe seleccionar un lote para asignar una posición.",
                    "ERP:LOT_REQUIRED");
            }

            var lotPosition = await _unitOfWork.LotsPositions.Entities
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == request.LotsPositionsId.Value && p.DeletedAt == null, cancellationToken);

            if (lotPosition == null)
            {
                return _errorManager.ThrowBadRequest<bool>(
                    "La posición del lote seleccionada no existe.",
                    "ERP:LOT_POSITION_NOT_FOUND");
            }

            if (lotPosition.LotId != lot.Id)
            {
                return _errorManager.ThrowBadRequest<bool>(
                    "La posición seleccionada no pertenece al lote elegido.",
                    "ERP:LOT_POSITION_NOT_IN_LOT");
            }

            if (lotPosition.IsBlocked)
            {
                return _errorManager.ThrowBadRequest<bool>(
                    "La posición seleccionada se encuentra bloqueada.",
                    "ERP:LOT_POSITION_BLOCKED");
            }

            var positionInUse = await _unitOfWork.WarehouseAssignments.Entities
                .AnyAsync(a => a.DeletedAt == null && a.LotsPositionsId == lotPosition.Id, cancellationToken);

            if (positionInUse)
            {
                return _errorManager.ThrowBadRequest<bool>(
                    "La posición seleccionada ya está ocupada.",
                    "ERP:LOT_POSITION_IN_USE");
            }
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

        var assignment = new WarehouseAssignmentEntity
        {
            Id = Guid.NewGuid(),
            RecordEntranceId = recordEntrance.Id,
            WarehouseId = warehouse.Id,
            SectionId = effectiveSectionId,
            RackId = rack.Id,
            LotsId = lot?.Id,
            LotsPositionsId = request.LotsPositionsId,
            RackPositionsId = request.RackPositionsId,
            AssignedAt = nowNica,
            AssignedByUserId = request.UserId.ToString()
        };

        var executionLog = await _unitOfWork.StepExecutionLogs.Entities
            .FirstOrDefaultAsync(l =>
                l.RecordEntranceId == recordEntrance.Id &&
                l.WorkflowStepDefinitionCode == WarehouseAssignmentRules.AssignmentStepCode,
                cancellationToken);

        if (executionLog == null)
        {
            executionLog = new StepExecutionLogs
            {
                Id = Guid.NewGuid(),
                RecordEntranceId = recordEntrance.Id,
                WorkflowStepDefinitionCode = WarehouseAssignmentRules.AssignmentStepCode,
                StartDate = today,
                StartTime = now,
                EndDate = null,
                EndTime = null,
                ProcessedByUserId = request.UserId.ToString(),
                ProcessedByUserName = currentUserName
            };
            await _unitOfWork.StepExecutionLogs.InsertExecutionLog(executionLog);
        }

        recordEntrance.CurrentStepCode = WarehouseAssignmentRules.AssignmentStepCode;

        await _unitOfWork.WarehouseAssignments.InsertWarehouseAssignment(assignment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}