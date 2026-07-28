
using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Commands;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;
using System.Net;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Handlers;

public class CreateReceptionEntranceHandler(
    IUnitOfWork _unitOfWork,
    IErrorManager _errorManager) 
    : IRequestHandler<CreateReceptionEntranceCommand, bool>
{
   public async Task<bool> Handle(CreateReceptionEntranceCommand request, CancellationToken cancellationToken)
    {
        var firstStep = await _unitOfWork.WorkflowStepDefinitions.Entities
            .OrderBy(x => x.ExecutionOrder)
            .FirstOrDefaultAsync(cancellationToken);

        if ( firstStep == null)
        {
            return _errorManager.ThrowInternalError<bool>(
                "No se encontró una configuración para el flujo de trabajo (WorkflowStepDefinition). Contacte al administrador.",
                "ERP:WORKFLOW_NOT_CONFIGURED");
        }

        var currentStepCode = firstStep.Code;

        #region 1. Validación DUCA duplicados en la misma peticion
        var duplicatesInRequest = request.DucatNumbers
            .GroupBy(d => d.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicatesInRequest.Any())
        {
            return _errorManager.ThrowBadRequest<bool>(
                $"La lista de DUCA's contiene números duplicados en la misma solicitud: {string.Join(", ", duplicatesInRequest)}",
                "ERP:REQUEST_DUPLICATED_DUCA");
        }
        #endregion

        #region 2. validacion DUCA unicidad global
        var normalizedRequestDucas = request.DucatNumbers
            .Select(d => d.Trim().ToLower())
            .ToList();

        var duplicateGlobalDucas = await _unitOfWork.EntranceDucats.Entities
            .Where(d => normalizedRequestDucas.Contains(d.DucatNumber.Trim().ToLower()))
            .Select(d => d.DucatNumber)
            .ToListAsync(cancellationToken);

        if (duplicateGlobalDucas.Any())
        {
            return _errorManager.ThrowBadRequest<bool>(
                $"Los siguientes números de DUCA ya están registrados en el sistema: {string.Join(", ", duplicateGlobalDucas)}. Cada DUCA debe ser único globalmente.",
                "ERP:GLOBAL_DUPLICATED_DUCA_ERROR");
        }
        #endregion

        #region 3. Validacion de Datos
        var startOfToday = DateTime.UtcNow.Date;
        var endOfToday = startOfToday.AddDays(1);

        var physicalDuplicateToday = await _unitOfWork.ReceptionEntrance.Entities
            .AnyAsync(r =>
                r.CreatedAt >= startOfToday && r.CreatedAt < endOfToday &&
                r.CountryOfOrigin.Trim().ToLower() == request.CountryOfOrigin.Trim().ToLower() &&
                r.Aduana.Trim().ToLower() == request.Aduana.Trim().ToLower() &&
                r.PlateNumber.Trim().ToLower() == request.PlateNumber.Trim().ToLower() &&
                r.TrailerChassis.Trim().ToLower() == request.TrailerChassis.Trim().ToLower() &&
                r.DriverLicense.Trim().ToLower() == request.DriverLicense.Trim().ToLower() &&
                r.Transportista.Trim().ToLower() == request.Transportista.Trim().ToLower() &&
                r.Medio.Trim().ToLower() == request.Medio.Trim().ToLower() &&
                r.DriverName.Trim().ToLower() == request.DriverName.Trim().ToLower() &&
                r.SealNumber.Trim().ToLower() == request.SealNumber.Trim().ToLower(),
                cancellationToken);

        if (physicalDuplicateToday)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "Ya se encuentra registrado un ingreso fisico idéntico para el día de hoy con los mismos datos.",
                "ERP:SAME_DAY_PHYSICAL_DUPLICATED");
        }
        #endregion

        #region 4. Registro de datos en db
        var recordEntranceId = Guid.NewGuid();

        var nowNica = NicaraguaClock.Now;
        var systemEndDate = DateOnly.FromDateTime(nowNica);
        var systemEndTime = TimeOnly.FromDateTime(nowNica);

        bool isConsolidated = request.DucatNumbers.Count > 1;

        var user = await _unitOfWork.Users.Entities
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "No se pudo identificar al usuario autenticado en el sistema.",
                "ERP:USER_NOT_FOUND");
        }

        var processedByUserName = user.Fullname ?? user.UserName ?? request.UserId.ToString();

        var recordEntrance = request.ToRecordEntranceEntity(recordEntranceId, isConsolidated, currentStepCode);
        var receptionEntrance = request.ToReceptionEntranceEntity(recordEntranceId);
        var executionLog = request.ToStepExecutionLogEntity(recordEntranceId, systemEndDate, systemEndTime, currentStepCode, processedByUserName);

        await _unitOfWork.RecordEntrance.InsertRecordEntrance(recordEntrance);
        await _unitOfWork.ReceptionEntrance.InsertReceptionEntrance(receptionEntrance);
        await _unitOfWork.StepExecutionLogs.InsertExecutionLog(executionLog);

        var ducatEntities = request.DucatNumbers
            .Select(ducatNumber => ducatNumber.ToEntranceDucaEntity(recordEntranceId))
            .ToList();

        await _unitOfWork.EntranceDucats.InsertEntranceDucatsRange(ducatEntities);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        #endregion
        
        return true;
    }
}