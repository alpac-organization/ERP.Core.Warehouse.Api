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

            if (access.Role?.RoleType == RoleType.Supervisor)
            {
                return _errorManager.ThrowUnauthorized<Unit>("No tienes acceso a realizar esta acción","ERP:INVALID_ACCESS");
            }

            var receptionEntrance = await _unitOfWork.ReceptionEntrance.Entities
                .Where(reception => reception.IsActive)
                .Where(reception => reception.Id == request.ReceptionEntranceId)
                .FirstOrDefaultAsync(cancellationToken);

            if (receptionEntrance is null)
            {
                return _errorManager.ThrowNotFound<Unit>("La reception a actualizar no existe registrada", "ERP:NOT_FOUND_RECEPTION");
            }
            
            var minutesElapsed = (DateTime.UtcNow - receptionEntrance.CreatedAt)
                .TotalMinutes;

            if (minutesElapsed >= 10 && (access.Role?.RoleType != RoleType.Administrator || access.Role?.RoleType != RoleType.Manager))
            {
                return _errorManager.ThrowBadRequest<Unit>(
                    "Ya no se puede modificar la información vehicular de la recepción",
                    "ERP:RECEPTION_UPDATE_TIME_EXPIRED"
                );
            }

            var additionalData = DeserializeAdditionalData(receptionEntrance.AdditionalData);
            var currentDocumentType = GetCurrentDocumentType(additionalData);

            if (request.GeneralInformation is not null)
            {
                receptionEntrance.SealNumber = request.GeneralInformation.SealNumber ?? receptionEntrance.SealNumber;
                receptionEntrance.CountryOfOrigin = request.GeneralInformation.CountryOrigin ?? receptionEntrance.CountryOfOrigin;
                receptionEntrance.ContainerNumber = request.GeneralInformation.ContainerNumber ?? receptionEntrance.ContainerNumber;

                if (request.GeneralInformation.CustomBranchId != Guid.Empty)
                {
                    var customBranch = await _unitOfWork.CustomerBranches.Entities
                        .Where(cb => cb.IsActive)
                        .Where(cb => cb.Id == request.GeneralInformation.CustomBranchId)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (customBranch is null)
                    {
                        return _errorManager.ThrowBadRequest<Unit>("La aduana de procendencia que desea actualizar no esta registrada", "ERP:ERROR_CUSTOM_BRANCH");
                    }

                    receptionEntrance.CustomBranchId = request.GeneralInformation.CustomBranchId;
                }

                var newDocumentType = request.GeneralInformation.DocumentType;
                
                if (currentDocumentType != newDocumentType)
                {
                    var validationResult = ValidateDocumentTypeChange(currentDocumentType, newDocumentType, request.GeneralInformation);

                    if (!validationResult.IsSuccess)
                    {
                        return (Unit)validationResult.ErrorResponse!;
                    }

                    UpdateDocumentNumbersInAdditionalData(additionalData, newDocumentType, request.GeneralInformation);
                }
                else
                {
                    UpdateDocumentNumbersInAdditionalData(additionalData, newDocumentType, request.GeneralInformation);
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

        private Result ValidateDocumentTypeChange(DocumentType? currentType, DocumentType newType, GeneralInformationUpdated generalInfo)
        {
            if (newType == DocumentType.DUCA)
            {
                if (generalInfo.DucatNumbers.Count == 0)
                {
                    return Result.Failure(_errorManager.ThrowBadRequest<Unit>("Debe proporcionar al menos un número de DUCA", "ERP:MISSING_DUCA_NUMBERS"));
                }
            }
            else if (newType == DocumentType.CustomsDeclaration)
            {
                if (string.IsNullOrWhiteSpace(generalInfo.CustomsDeclarationNumber))
                {
                    return Result.Failure(_errorManager.ThrowBadRequest<Unit>("El número de declaración aduanera es obligatorio", "ERP:MISSING_CUSTOMS_DECLARATION"));
                }
            }
            else
            {
                return Result.Failure(_errorManager.ThrowBadRequest<Unit>("Tipo de documento no válido", "ERP:INVALID_DOCUMENT_TYPE"));
            }

            return Result.Success();
        }

        private static void UpdateDocumentNumbersInAdditionalData(AdditionalReceptionEntranceData additionalData, DocumentType newType, GeneralInformationUpdated generalInfo)
        {
            var currentType = GetCurrentDocumentType(additionalData);
            
            if (currentType == DocumentType.DUCA && newType == DocumentType.CustomsDeclaration)
            {
                additionalData.DocumentNumbers.RemoveAll(d => d.DocumentType == DocumentType.DUCA);
                
                if (!string.IsNullOrWhiteSpace(generalInfo.CustomsDeclarationNumber))
                {
                    additionalData.DocumentNumbers.Add(new DocumentInformation
                    {
                        DocumentId = Guid.NewGuid(),
                        DocumentType = DocumentType.CustomsDeclaration,
                        DocumentNumbers = generalInfo.CustomsDeclarationNumber
                    });
                }
            }
            else if (currentType == DocumentType.CustomsDeclaration && newType == DocumentType.DUCA)
            {
                additionalData.DocumentNumbers.RemoveAll(d => d.DocumentType == DocumentType.CustomsDeclaration);
                
                foreach (var duca in generalInfo.DucatNumbers)
                {
                    additionalData.DocumentNumbers.Add(new DocumentInformation
                    {
                        DocumentId = Guid.NewGuid(),
                        DocumentType = DocumentType.DUCA,
                        DocumentNumbers = duca
                    });
                }
            }
            else if (currentType == newType)
            {
                if (newType == DocumentType.DUCA)
                {
                    additionalData.DocumentNumbers.RemoveAll(d => d.DocumentType == DocumentType.DUCA);
                    foreach (var duca in generalInfo.DucatNumbers)
                    {
                        additionalData.DocumentNumbers.Add(new DocumentInformation
                        {
                            DocumentId = Guid.NewGuid(),
                            DocumentType = DocumentType.DUCA,
                            DocumentNumbers = duca
                        });
                    }
                }
                else if (newType == DocumentType.CustomsDeclaration)
                {
                    additionalData.DocumentNumbers.RemoveAll(d => d.DocumentType == DocumentType.CustomsDeclaration);
                    if (!string.IsNullOrWhiteSpace(generalInfo.CustomsDeclarationNumber))
                    {
                        additionalData.DocumentNumbers.Add(new DocumentInformation
                        {
                            DocumentId = Guid.NewGuid(),
                            DocumentType = DocumentType.CustomsDeclaration,
                            DocumentNumbers = generalInfo.CustomsDeclarationNumber
                        });
                    }
                }
            }
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