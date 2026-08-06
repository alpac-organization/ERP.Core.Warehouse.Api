using Microsoft.EntityFrameworkCore;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Commons.Constants;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Handlers;

public class AddDucatsToReceptionHandler(
    IUnitOfWork unitOfWork,
    IErrorManager errorManager)
    : BaseValidatorHandler<AddDucatsToReceptionCommand, bool>(unitOfWork, errorManager)
{
    public override async Task<bool> Handle(AddDucatsToReceptionCommand request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        #region 1. Verificar que el registro padre exista
        var recordEntrance = await _unitOfWork.RecordEntrance.Entities
            .Include(r => r.ReceptionEntrance)
            .Include(r => r.EntranceDucats.Where(d => d.DeletedAt == null))
            .FirstOrDefaultAsync(r => r.Id == request.ReceptionId && r.DeletedAt == null, cancellationToken);

        if (recordEntrance == null)
            return _errorManager.ThrowBadRequest<bool>(
                "El registro de recepción no fue encontrado o ya ha sido eliminado.",
                "ERP:RECEPTION_NOT_FOUND");
        #endregion

        #region 1.a Validar que el expediente sea tipo DUCA
        if (recordEntrance.ReceptionEntrance?.DocumentType != DocumentType.DUCA)
            return _errorManager.ThrowBadRequest<bool>(
                "No se pueden agregar DUCA's a un expediente que no es de tipo DUCA.",
                "ERP:INVALID_DOCUMENT_TYPE_FOR_DUCA");
        #endregion

        #region  1.b Restringir si el expediente ya avanzo de paso
        var hasAdvanced = await _unitOfWork.StepExecutionLogs.Entities
            .AnyAsync(l => l.RecordEntranceId == request.ReceptionId
                        && l.WorkflowStepDefinitionCode != WorkflowStepCodes.Reception
                        && l.DeletedAt == null,
                        cancellationToken);

        if (hasAdvanced)
            return _errorManager.ThrowInternalError<bool>(
                "No es posible agregar DUCA's porque el expediente ya avanzó a un paso posterior del proceso.",
                "ERP:RECORD_ALREADY_ADVANCED");
        #endregion

        #region 2. Validar duplicados en la misma pericion
        var normalizedRequestDucas = request.DucatNumbers
            .Select(d => d.ToLower())
            .ToList();

        var duplicatesInRequest = normalizedRequestDucas
            .GroupBy(d => d)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicatesInRequest.Any())
            return _errorManager.ThrowBadRequest<bool>(
                $"La lista de DUCA's contiene números duplicados en la misma solicitud: {string.Join(", ", duplicatesInRequest)}",
                "ERP:REQUEST_DUPLICATED_DUCA");
        #endregion

        #region 3. Validar unicidad global (excluyendo los ya existentes en este registro)
        var existingDucatNumbers = recordEntrance.EntranceDucats
            .Select(d => d.DucatNumber.ToLower())
            .ToHashSet();

        var newDucatNumbers = normalizedRequestDucas
            .Where(d => !existingDucatNumbers.Contains(d))
            .ToList();

        #region 3a. Validar si los enviados existen en el registro
        var alreadyInThisRecord = normalizedRequestDucas
            .Where(d => existingDucatNumbers.Contains(d))
            .ToList();

        if (alreadyInThisRecord.Any())
            return _errorManager.ThrowBadRequest<bool>(
                $"Los siguientes DUCA's ya pertenecen a este registro de recepción: {string.Join(", ", alreadyInThisRecord)}",
                "ERP:DUCA_ALREADY_IN_RECORD");
        #endregion

        #region 3b. Validar duplicados contra el resto del sistema
        var duplicateGlobalDucas = await _unitOfWork.EntranceDucats.Entities
            .Where(d => d.DeletedAt == null && newDucatNumbers.Contains(d.DucatNumber.ToLower()))
            .Select(d => d.DucatNumber)
            .ToListAsync(cancellationToken);

        if (duplicateGlobalDucas.Any())
            return _errorManager.ThrowBadRequest<bool>(
                $"Los siguietes números DUCA ya están registrados en el sistema: {string.Join(", ", duplicateGlobalDucas)}. Cada DUCA debe ser único.",
                "ERP:GLOBAL_DUPLICATED_DUCA_ERROR");
        #endregion
        #endregion

        #region 4. Insertar los nuevos DUCA's
        var ducatEntities = request.DucatNumbers
            .Select(ducatNumber =>
                ducatNumber.ToEntranceDucaEntity(request.ReceptionId))
            .ToList();

        await _unitOfWork.EntranceDucats.InsertEntranceDucatsRange(ducatEntities);
        #endregion

        #region 5. Recalcular isconsolidated del expediente
        var totalDucas = recordEntrance.EntranceDucats.Count(d => d.DeletedAt == null) + ducatEntities.Count;

        if (totalDucas > 1 && !recordEntrance.IsConsolidated)
        {
            recordEntrance.IsConsolidated = true;
        }
        #endregion

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}