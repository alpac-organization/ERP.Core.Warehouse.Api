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
                return _errorManager.ThrowUnauthorized<Unit>("No tienes acceso a realizar esta acción", "ERP:INVALID_ACCESS");
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

            var minutesElapsed = (DateTime.UtcNow - receptionEntrance.CreatedAt).TotalMinutes;

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

            // Actualizar imagenes o evidencia fotografica.
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
                    return _errorManager.ThrowBadRequest<Unit>("La información de transporte no se ha encontrado", "ERP:ERROR_UPDATED");
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

        /// <summary>
        /// Sincroniza los números de documento entre las OperationalOrders y el AdditionalData.
        /// Ahora opera por OperationalOrderId, no por comparación de strings.
        /// </summary>
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

            var currentType = operationalOrders.FirstOrDefault()?.DocumentType
                              ?? GetCurrentDocumentType(additionalData);

            var effectiveType = generalInfo.DocumentType
                                ?? (hasDucas ? DocumentType.DUCA
                                    : hasCustomsDeclaration ? DocumentType.CustomsDeclaration
                                    : currentType);

            if (effectiveType is null)
            {
                return Result.Failure(_errorManager.ThrowBadRequest<Unit>(
                    "No fue posible determinar el tipo de documento de la recepción",
                    "ERP:INVALID_DOCUMENT_TYPE"));
            }

            // ---------- Rama DUCA ----------
            if (effectiveType == DocumentType.DUCA && hasDucas)
            {
                var updates = generalInfo.DucatNumbers
                    .Where(d => d.OperationalOrderId != Guid.Empty
                                && !string.IsNullOrWhiteSpace(d.DocumentNumber))
                    .Select(d => new { d.OperationalOrderId, Number = d.DocumentNumber.Trim() })
                    .ToList();

                if (updates.Count == 0)
                {
                    return Result.Success();
                }

                // Duplicados de números
                if (updates.Select(u => u.Number).Distinct(StringComparer.OrdinalIgnoreCase).Count() != updates.Count)
                {
                    return Result.Failure(_errorManager.ThrowBadRequest<Unit>(
                        "La lista de números de DUCA contiene valores duplicados",
                        "ERP:DUPLICATE_DOCUMENT_NUMBERS"));
                }

                // Duplicados de IDs
                if (updates.Select(u => u.OperationalOrderId).Distinct().Count() != updates.Count)
                {
                    return Result.Failure(_errorManager.ThrowBadRequest<Unit>(
                        "La lista contiene OperationalOrderId duplicados",
                        "ERP:DUPLICATE_OPERATIONAL_ORDER_IDS"));
                }

                var validIds = operationalOrders.Select(o => o.Id).ToHashSet();
                var invalidIds = updates.Where(u => !validIds.Contains(u.OperationalOrderId)).ToList();

                if (invalidIds.Count > 0)
                {
                    return Result.Failure(_errorManager.ThrowBadRequest<Unit>(
                        "Uno o más OperationalOrderId no pertenecen a esta recepción",
                        "ERP:INVALID_OPERATIONAL_ORDER_ID"));
                }

                // Aplicar cambios
                foreach (var update in updates)
                {
                    var order = operationalOrders.First(o => o.Id == update.OperationalOrderId);

                    order.DocumentNumber = update.Number;
                    order.DocumentType = DocumentType.DUCA;

                    var docEntry = additionalData.DocumentNumbers
                        .FirstOrDefault(d => d.OperationalOrderId == update.OperationalOrderId);

                    if (docEntry is not null)
                    {
                        docEntry.DocumentNumbers = update.Number;
                        docEntry.DocumentType = DocumentType.DUCA;
                    }
                    else
                    {
                        additionalData.DocumentNumbers.Add(new DocumentInformation
                        {
                            DocumentId = Guid.NewGuid(),
                            OperationalOrderId = update.OperationalOrderId,
                            DocumentType = DocumentType.DUCA,
                            DocumentNumbers = update.Number
                        });
                    }
                }

                var isConsolidated = operationalOrders.Count > 1;
                foreach (var order in operationalOrders)
                {
                    order.IsConsolidated = isConsolidated;
                }

                // Reordenar AdditionalData.DocumentNumbers para que coincida con el orden de las OPs
                var orderIds = operationalOrders.Select(o => o.Id).ToList();
                additionalData.DocumentNumbers = additionalData.DocumentNumbers
                    .Where(d => d.DocumentType == DocumentType.DUCA)
                    .OrderBy(d =>
                    {
                        var idx = orderIds.IndexOf(d.OperationalOrderId);
                        return idx < 0 ? int.MaxValue : idx;
                    })
                    .ToList();
            }
            // ---------- Rama CustomsDeclaration ----------
            else if (effectiveType == DocumentType.CustomsDeclaration && hasCustomsDeclaration)
            {
                if (operationalOrders.Count != 1)
                {
                    return Result.Failure(_errorManager.ThrowBadRequest<Unit>(
                        "Una recepción con declaración aduanera debe tener exactamente una orden operativa",
                        "ERP:DOCUMENT_COUNT_MISMATCH"));
                }

                var order = operationalOrders[0];
                var newNumber = generalInfo.CustomsDeclarationNumber!.Trim();

                order.DocumentNumber = newNumber;
                order.DocumentType = DocumentType.CustomsDeclaration;
                order.IsConsolidated = false;

                var docEntry = additionalData.DocumentNumbers
                    .FirstOrDefault(d => d.OperationalOrderId == order.Id);

                if (docEntry is not null)
                {
                    docEntry.DocumentNumbers = newNumber;
                    docEntry.DocumentType = DocumentType.CustomsDeclaration;
                }
                else
                {
                    // Fallback: buscar por tipo cuando el AdditionalData venga de un flujo sin OperationalOrderId
                    var legacyEntry = additionalData.DocumentNumbers
                        .FirstOrDefault(d => d.DocumentType == DocumentType.CustomsDeclaration);

                    if (legacyEntry is not null)
                    {
                        legacyEntry.OperationalOrderId = order.Id;
                        legacyEntry.DocumentNumbers = newNumber;
                    }
                    else
                    {
                        additionalData.DocumentNumbers.Add(new DocumentInformation
                        {
                            DocumentId = Guid.NewGuid(),
                            OperationalOrderId = order.Id,
                            DocumentType = DocumentType.CustomsDeclaration,
                            DocumentNumbers = newNumber
                        });
                    }
                }
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