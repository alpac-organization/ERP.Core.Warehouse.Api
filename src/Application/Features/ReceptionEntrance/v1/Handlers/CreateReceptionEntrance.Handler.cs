using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Application.Commons.Interfaces.AWS;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Commons.Constants;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Commands;
using ReceptionEntranceEntity = ERP.Core.Database.Domain.Entities.Warehouse.ReceptionEntrance;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Handlers;

public class CreateReceptionEntranceHandler(
    IUnitOfWork unitOfWork,
    IErrorManager errorManager,
    IMapper mapper,
    IS3StorageService s3StorageService)
    : BaseValidatorHandler<CreateReceptionEntranceCommand, bool>(unitOfWork, errorManager)
{
    public override async Task<bool> Handle(CreateReceptionEntranceCommand request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        #region 0. Paso de workflow explícito (Recepción)
        var currentStepCode = WorkflowStepCodes.Reception;

        var stepIsConfigured = await _unitOfWork.WorkflowStepDefinitions.Entities
            .AnyAsync(x => x.Code == currentStepCode, cancellationToken);

        if (!stepIsConfigured)
        {
            return _errorManager.ThrowInternalError<bool>(
                $"No se encontró la configuración del paso '{WorkflowStepCodes.Reception}' en WorkflowStepDefinitions. Contacte al administrador.",
                "ERP:WORKFLOW_NOT_CONFIGURED");
        }
        #endregion

        bool isDuca = request.DocumentType == DocumentType.DUCA;

        #region 1. Validación DUCA (solo aplica si DocumentType == DUCA)
        if (isDuca)
        {
            #region 1.a Validación DUCA duplicados en la misma peticion
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

            #region 1.b validacion DUCA unicidad global
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
        }
        else
        {
            #region 1.c DocumentType == CustomsDeclaration: validar unicidad del numero de declaracion
            var declarationExists = await _unitOfWork.CustomsDeclarations.Entities
                .AnyAsync(d => d.CustomsDeclarationNumber.Trim().ToLower()
                            == request.CustomsDeclarationNumber!.Trim().ToLower(), cancellationToken);

            if (declarationExists)
            {
                return _errorManager.ThrowBadRequest<bool>(
                    $"El número de declaración aduanera {request.CustomsDeclarationNumber} ya está registrado en el sistema.",
                    "ERP:GLOBAL_DUPLICATED_CUSTOMS_DECLARATION");
            }
            #endregion
        }
        #endregion

        #region 2. Validacion de Datos (ingreso fisico duplicado el mismo dia)
        var startOfToday = DateTime.UtcNow.Date;
        var endOfToday = startOfToday.AddDays(1);

        var physicalDuplicateToday = await _unitOfWork.ReceptionEntrance.Entities
            .AnyAsync(r =>
                r.CreatedAt >= startOfToday && r.CreatedAt < endOfToday &&
                r.CountryOfOrigin.Trim().ToLower() == request.CountryOfOrigin.Trim().ToLower() &&
                r.CustomBranchId == request.CustomBranchId &&
                r.VehiclePlateNumber.Trim().ToLower() == request.VehiclePlateNumber.Trim().ToLower() &&
                r.VehicleChassisNumber.Trim().ToLower() == request.VehicleChassisNumber.Trim().ToLower() &&
                r.DriverLicense.Trim().ToLower() == request.DriverLicense.Trim().ToLower() &&
                r.Transportista.Trim().ToLower() == request.Transportista.Trim().ToLower() &&
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

        #region 3. Registro de datos en db
        var recordEntranceId = Guid.NewGuid();

        var nowNica = NicaraguaClock.Now;
        var systemEndDate = DateOnly.FromDateTime(nowNica);
        var systemEndTime = TimeOnly.FromDateTime(nowNica);

        bool isConsolidated = isDuca && request.DucatNumbers.Count > 1;

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

        var evidenceUrls = new List<string>();

        try
        {
            // PASO 1: Subir imágenes a S3 PRIMERO
            if (request.EvidenceBase64 != null && request.EvidenceBase64.Count > 0)
            {
                evidenceUrls = (await s3StorageService.UploadImagesAsync(
                    module: S3Sections.Module,
                    section: S3Sections.ReceptionEvidence,
                    base64Images: request.EvidenceBase64,
                    cancellationToken: cancellationToken)).ToList();
            }

            // PASO 2: Mapear entidades
            var contextItems = new Dictionary<string, object>
            {
                ["RecordEntranceId"] = recordEntranceId,
                ["StepCode"] = currentStepCode,
                ["IsConsolidated"] = isConsolidated,
                ["EndDate"] = systemEndDate,
                ["EndTime"] = systemEndTime,
                ["ProcessedByUserName"] = processedByUserName,
                ["EvidenceUrls"] = evidenceUrls
            };

            var recordEntrance = mapper.Map<RecordEntrance>(request, opts =>
            {
                foreach (var item in contextItems)
                {
                    opts.Items[item.Key] = item.Value;
                }
            });

            var receptionEntrance = mapper.Map<ReceptionEntranceEntity>(request, opts =>
            {
                foreach (var item in contextItems)
                {
                    opts.Items[item.Key] = item.Value;
                }
            });

            var executionLog = mapper.Map<StepExecutionLogs>(request, opts =>
            {
                foreach (var item in contextItems)
                {
                    opts.Items[item.Key] = item.Value;
                }
            });

            // PASO 3: Insertar en BD
            await _unitOfWork.RecordEntrance.InsertRecordEntrance(recordEntrance);
            await _unitOfWork.ReceptionEntrance.InsertReceptionEntrance(receptionEntrance);
            await _unitOfWork.StepExecutionLogs.InsertExecutionLog(executionLog);

            if (isDuca)
            {
                var ducatEntities = request.DucatNumbers
                    .Select(ducatNumber => ducatNumber.ToEntranceDucaEntity(recordEntranceId))
                    .ToList();

                await _unitOfWork.EntranceDucats.InsertEntranceDucatsRange(ducatEntities);
            }
            else
            {
                var customsDeclaration = mapper.Map<CustomsDeclarations>(request, opts =>
                {
                    opts.Items["RecordEntranceId"] = recordEntranceId;
                });

                await _unitOfWork.CustomsDeclarations.RegisterCustomsDeclarations(customsDeclaration);

                var customsDeclarationDetails = mapper.Map<CustomsDeclarationDetails>(request, opts =>
                {
                    opts.Items["CustomsDeclarationId"] = customsDeclaration.Id;
                });

                await _unitOfWork.CustomsDeclarationDetails.RegisterCustomsDeclarationDetails(customsDeclarationDetails);
            }

            // PASO 4: Guardar en BD
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception)
        {
            // Verificar si el registro realmente se guardó en BD
            var recordExists = await _unitOfWork.RecordEntrance.Entities
                .AsNoTracking()
                .AnyAsync(r => r.Id == recordEntranceId, cancellationToken);

            // Solo eliminar imágenes de S3 si el registro NO existe en BD
            if (!recordExists && evidenceUrls.Count > 0)
            {
                try
                {
                    await s3StorageService.DeleteImagesAsync(evidenceUrls, cancellationToken);
                }
                catch
                {
                    // Si S3 también falla, las imágenes quedarán huérfanas
                }
            }

            throw;
        }
        #endregion

        return true;
    }
}