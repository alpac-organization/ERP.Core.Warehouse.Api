using MediatR;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Application.Commons.Interfaces.AWS;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Handlers
{
    public class UpdateReceptionEntranceHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, IS3StorageService _s3StorageService) : BaseValidatorHandler<UpdateReceptionEntranceCommand, Unit>(_unitOfWork, _errorManager)
    {
        private static readonly JsonSerializerOptions SnakeCaseOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        public override async Task<Unit> Handle(UpdateReceptionEntranceCommand request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
            
            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            if (access.Role?.RoleType is not (RoleType.Administrator or RoleType.Supervisor or RoleType.Manager))
            {
                return _errorManager.ThrowUnauthorized<Unit>("No tienes acceso a realizar esta acción","ERP:INVALID_ACCESS");
            }

            var receptionEntrance = await _unitOfWork.ReceptionEntrance.Entities
                .Where(reception => reception.IsActive)
                .Where(reception => reception.Id == request.ReceptionEntranceId)
                .Include(reception => reception.OperationalOrders)
                .FirstOrDefaultAsync(cancellationToken);

            if (receptionEntrance is null)
            {
                return _errorManager.ThrowNotFound<Unit>("La reception a actualizar no existe registrada", "ERP:NOT_FOUND_RECEPTION");
            }
            
            var minutesElapsed = (DateTime.UtcNow - receptionEntrance.CreatedAt)
                .TotalMinutes;

            if (minutesElapsed >= 10 && access.Role?.RoleType is not (RoleType.Administrator or RoleType.Manager))
            {
                return _errorManager.ThrowBadRequest<Unit>(
                    "Ya no se puede modificar la información vehicular de la recepción",
                    "ERP:RECEPTION_UPDATE_TIME_EXPIRED"
                );
            }

            var additionalData = DeserializeAdditionalData(receptionEntrance.AdditionalData);

            if (request.GeneralInformation is not null)
            {
                var documentSync = SyncDocuments(receptionEntrance, additionalData, request.GeneralInformation);

                if (!documentSync.IsSuccess)
                {
                    return (Unit)documentSync.ErrorResponse!;
                }

                receptionEntrance.SealNumber = request.GeneralInformation.SealNumber ?? receptionEntrance.SealNumber;
                receptionEntrance.CountryOfOrigin = request.GeneralInformation.CountryOrigin ?? receptionEntrance.CountryOfOrigin;
                receptionEntrance.ContainerNumber = request.GeneralInformation.ContainerNumber ?? receptionEntrance.ContainerNumber;

                if (request.GeneralInformation.CustomBranchId != Guid.Empty)
                {
                    var customBranch = await _unitOfWork.CustomsBranches.Entities
                        .Where(cb => cb.IsActive)
                        .Where(cb => cb.Id == request.GeneralInformation.CustomBranchId)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (customBranch is null)
                    {
                        return _errorManager.ThrowBadRequest<Unit>("La aduana de procendencia que desea actualizar no esta registrada", "ERP:ERROR_CUSTOM_BRANCH");
                    }

                    receptionEntrance.CustomBranchId = request.GeneralInformation.CustomBranchId;
                }
            }

            //Actualizar imagenes o evidencia fotografica.
            if (request.EvidenceBase64.Count > 0 || request.EvidenceIdsToDelete.Count > 0)
            {
                await UpdateEvidenceImagesAsync(additionalData, request.EvidenceBase64, request.EvidenceIdsToDelete);
            }

            receptionEntrance.AdditionalData = JsonSerializer.Serialize(additionalData, SnakeCaseOptions);
            
            await _unitOfWork.ReceptionEntrance.UpdateAsync(receptionEntrance);

            if (request.ReceptionTransportInformation is not null)
            {
                var receptionTransportEntrance = await _unitOfWork.ReceptionTransportEntrance.Entities
                    .Where(reception => reception.ReceptionEntranceId == receptionEntrance.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (receptionTransportEntrance is null)
                {
                    return _errorManager.ThrowBadRequest<Unit>("La información de transporte no se ha encontrado","ERP:ERROR_UPDATED");
                }

                receptionTransportEntrance.DriverName = request.ReceptionTransportInformation?.DriverName ?? receptionTransportEntrance.DriverName;
                receptionTransportEntrance.Transportista = request.ReceptionTransportInformation?.Transportista ?? receptionTransportEntrance.Transportista;
                receptionTransportEntrance.DriverLicense = request.ReceptionTransportInformation?.DriverLicense ?? receptionTransportEntrance.DriverLicense;
                receptionTransportEntrance.VehiclePlateNumber = request.ReceptionTransportInformation?.VehiclePlateNumber ?? receptionTransportEntrance.VehiclePlateNumber;
                receptionTransportEntrance.VehicleChassisNumber = request.ReceptionTransportInformation?.VehicleChassisNumber ?? receptionTransportEntrance.VehicleChassisNumber;

                if (request.ReceptionTransportInformation?.TransportUnit is not null)
                {
                    receptionTransportEntrance.TransportUnit = request.ReceptionTransportInformation.TransportUnit.Value;
                }

                await _unitOfWork.ReceptionTransportEntrance.UpdateAsync(receptionTransportEntrance);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }

        #region Deserializar AdditionalData
        
        private static AdditionalReceptionEntranceData DeserializeAdditionalData(string? additionalDataJson)
        {
            if (string.IsNullOrWhiteSpace(additionalDataJson))
            {
                return new AdditionalReceptionEntranceData();
            }

            return JsonSerializer.Deserialize<AdditionalReceptionEntranceData>(additionalDataJson, SnakeCaseOptions) ?? new AdditionalReceptionEntranceData();
        }

        #endregion

        #region Actualizar información de evidencia fotografica.
        private async Task UpdateEvidenceImagesAsync(AdditionalReceptionEntranceData additionalData, List<string> newEvidenceBase64, List<Guid> evidenceIdsToDelete)
        {
            if (evidenceIdsToDelete.Count > 0)
            {
                additionalData.EvidenceUrls.RemoveAll(img => evidenceIdsToDelete.Contains(img.ImageId));
            }

            foreach (var base64Image in newEvidenceBase64)
            {
                var imageUrl = await _s3StorageService.UploadImageAsync("Warehouse", "ReceptionEntrance", base64Image, default);
                var imageEntity = new ImagesInformation
                {
                    ImageId = Guid.NewGuid(),
                    ImageUrl = imageUrl
                };
                additionalData.EvidenceUrls.Add(imageEntity);
            }
        }
        #endregion

        
        private static DocumentType? GetCurrentDocumentType(AdditionalReceptionEntranceData additionalData)
        {
            var firstDoc = additionalData.DocumentNumbers.FirstOrDefault();
            return firstDoc?.DocumentType;
        }

        private Result SyncDocuments(
            ERP.Core.Database.Domain.Entities.Warehouse.ReceptionEntrance receptionEntrance,
            AdditionalReceptionEntranceData additionalData,
            GeneralInformationUpdated generalInfo)
        {
            var operationalOrders = receptionEntrance.OperationalOrders.ToList();

            var hasDucas = generalInfo.DucatNumbers is { Count: > 0 };
            var hasCustomsDeclaration = !string.IsNullOrWhiteSpace(generalInfo.CustomsDeclarationNumber);

            if (hasDucas && hasCustomsDeclaration)
            {
                return Result.Failure(_errorManager.ThrowBadRequest<Unit>(
                    "Envíe los números de DUCA o el número de declaración aduanera, no ambos",
                    "ERP:CONFLICTING_DOCUMENT_FIELDS"));
            }

            if (generalInfo.DocumentType is { } requestedType
                && requestedType is not (DocumentType.DUCA or DocumentType.CustomsDeclaration))
            {
                return Result.Failure(_errorManager.ThrowBadRequest<Unit>(
                    "Tipo de documento no válido",
                    "ERP:INVALID_DOCUMENT_TYPE"));
            }

            var inferredType = hasDucas
                ? DocumentType.DUCA
                : hasCustomsDeclaration
                    ? DocumentType.CustomsDeclaration
                    : (DocumentType?)null;

            var currentType = operationalOrders.FirstOrDefault()?.DocumentType
                              ?? GetCurrentDocumentType(additionalData);

            var effectiveType = generalInfo.DocumentType ?? inferredType ?? currentType;

            if (effectiveType is null)
            {
                return Result.Failure(_errorManager.ThrowBadRequest<Unit>(
                    "No fue posible determinar el tipo de documento de la recepción",
                    "ERP:INVALID_DOCUMENT_TYPE"));
            }

            var desiredNumbers = effectiveType == DocumentType.DUCA
                ? generalInfo.DucatNumbers
                    .Where(number => !string.IsNullOrWhiteSpace(number))
                    .Select(number => number.Trim())
                    .ToList()
                : hasCustomsDeclaration
                    ? [generalInfo.CustomsDeclarationNumber!.Trim()]
                    : [];

            if (desiredNumbers.Count == 0)
            {
                return Result.Success();
            }

            if (desiredNumbers.Distinct(StringComparer.OrdinalIgnoreCase).Count() != desiredNumbers.Count)
            {
                return Result.Failure(_errorManager.ThrowBadRequest<Unit>(
                    "La lista de números de documento contiene valores duplicados",
                    "ERP:DUPLICATE_DOCUMENT_NUMBERS"));
            }

            var isTypeChange = currentType is not null && currentType != effectiveType;

            if (isTypeChange && operationalOrders.Count > 1)
            {
                return Result.Failure(_errorManager.ThrowBadRequest<Unit>(
                    "No se puede cambiar el tipo de documento cuando la recepción tiene más de un documento",
                    "ERP:DOCUMENT_TYPE_CHANGE_NOT_ALLOWED"));
            }

            if (desiredNumbers.Count != operationalOrders.Count)
            {
                return Result.Failure(_errorManager.ThrowBadRequest<Unit>(
                    $"La recepción tiene {operationalOrders.Count} documento(s) y se recibieron {desiredNumbers.Count}. " +
                    "No se pueden agregar ni eliminar documentos mediante esta operación",
                    "ERP:DOCUMENT_COUNT_MISMATCH"));
            }

            var currentNumbers = operationalOrders
                .Select(operationalOrder => operationalOrder.DocumentNumber)
                .Where(number => !string.IsNullOrWhiteSpace(number))
                .Select(number => number!.Trim())
                .ToList();

            var removed = currentNumbers
                .Where(number => !desiredNumbers.Contains(number, StringComparer.OrdinalIgnoreCase))
                .ToList();

            var added = desiredNumbers
                .Where(number => !currentNumbers.Contains(number, StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (removed.Count > 1 || added.Count > 1)
            {
                return Result.Failure(_errorManager.ThrowBadRequest<Unit>(
                    "Solo se puede cambiar un número de documento por operación",
                    "ERP:MULTIPLE_DOCUMENT_RENAMES"));
            }

            if (removed.Count == 1 && added.Count == 1)
            {
                var previousNumber = removed[0];
                var newNumber = added[0];

                var matchingEntry = additionalData.DocumentNumbers.FirstOrDefault(document =>
                    string.Equals(document.DocumentNumbers, previousNumber, StringComparison.OrdinalIgnoreCase));

                foreach (var operationalOrder in operationalOrders)
                {
                    if (string.Equals(operationalOrder.DocumentNumber, previousNumber, StringComparison.OrdinalIgnoreCase))
                    {
                        operationalOrder.DocumentNumber = newNumber;
                    }
                }

                if (matchingEntry is not null)
                {
                    matchingEntry.DocumentNumbers = newNumber;
                }
                else
                {
                    additionalData.DocumentNumbers.Add(new DocumentInformation
                    {
                        DocumentId = Guid.NewGuid(),
                        DocumentType = effectiveType.Value,
                        DocumentNumbers = newNumber
                    });
                }
            }

            if (isTypeChange)
            {
                foreach (var documentEntry in additionalData.DocumentNumbers)
                {
                    documentEntry.DocumentType = effectiveType.Value;
                }
            }

            foreach (var operationalOrder in operationalOrders)
            {
                operationalOrder.DocumentType = effectiveType.Value;
                operationalOrder.IsConsolidated = desiredNumbers.Count > 1;
            }

            additionalData.DocumentNumbers = additionalData.DocumentNumbers
                .Where(document => !string.IsNullOrWhiteSpace(document.DocumentNumbers))
                .GroupBy(document => document.DocumentNumbers!.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderBy(document => desiredNumbers.FindIndex(number =>
                    string.Equals(number, document.DocumentNumbers, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var synchronizedNumbers = operationalOrders
                .Select(operationalOrder => operationalOrder.DocumentNumber)
                .Where(number => !string.IsNullOrWhiteSpace(number))
                .Select(number => number!.Trim())
                .OrderBy(number => number, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var expectedNumbers = desiredNumbers
                .OrderBy(number => number, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (additionalData.DocumentNumbers.Count != desiredNumbers.Count
                || !synchronizedNumbers.SequenceEqual(expectedNumbers, StringComparer.OrdinalIgnoreCase))
            {
                return Result.Failure(_errorManager.ThrowBadRequest<Unit>(
                    "La información de documentos no pudo sincronizarse de forma consistente",
                    "ERP:DOCUMENT_SYNC_INCONSISTENT"));
            }

            return Result.Success();
        }

        private class Result
        {
            public bool IsSuccess { get; private set; }
            public Unit? ErrorResponse { get; private set; }

            private Result(bool isSuccess, Unit? errorResponse = null)
            {
                IsSuccess = isSuccess;
                ErrorResponse = errorResponse;
            }

            public static Result Success() => new(true);
            public static Result Failure(Unit errorResponse) => new(false, errorResponse);
        }
    }
}