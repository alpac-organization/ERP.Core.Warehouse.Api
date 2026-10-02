using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Application.Commons.Interfaces.AWS;

using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Shopping;
using ERP.Core.Database.Domain.Entities.Auth;
using ERP.Core.Database.Domain.Entities.Bases;
using ERP.Core.Database.Domain.Entities.Catalogs;

using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Handlers
{
    public class UpdatePurchaseHandler(
        IUnitOfWork _unitOfWork,
        IErrorManager _errorManager,
        ILogger<UpdatePurchaseHandler> _logger,
        IS3StorageService _s3StorageService) : BaseValidatorHandler<UpdatePurchaseCommand, bool>(_unitOfWork, _errorManager)
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        };

        public override async Task<bool> Handle(UpdatePurchaseCommand request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            if (access.Role?.RoleType == RoleType.Supervisor)
            {
                return _errorManager.ThrowBadRequest<bool>("No tienes permiso para realizar esta acción", "ERP:INVALID_ACCESS");
            }

            _logger.LogInformation("🚩 Iniciando Actualizacion de solicitud de compra");

            var purchase = await _unitOfWork.PurchaseRequests.Entities
                .Include(p => p.PurchaseRequestItems)
                .Where(p => p.IsActive)
                .Where(p => p.Id == request.PurchaseRequestId)
                .FirstOrDefaultAsync(cancellationToken);

            if (purchase is null)
            {
                return _errorManager.ThrowNotFound<bool>("La cotización no existe.", "ERP:QUOTATION_NOT_FOUND");
            }

            if (purchase.RequestStatus != PurchaseRequestStatus.Pending)
            {
                return _errorManager.ThrowBadRequest<bool>("Solo se pueden actualizar solicitudes en estado pendiente.", "ERP:PURCHASE_REQUEST_NOT_PENDING");
            }

            var historyList = DeserializeHistory(purchase.AdditionalData);
            var hasChanges = false;

            var workArea = access.Profile.WorkArea
                ?? await _unitOfWork.WorkAreas.Entities
                    .AsNoTracking()
                    .FirstOrDefaultAsync(wa => wa.Id == access.Profile.AreaId, cancellationToken);

            if (request.PriorityLevel.HasValue)
            {
                if (purchase.RequestType == PurchaseRequestType.Requisition && (request.PriorityLevel.Value == PriorityLevel.None || !Enum.IsDefined(request.PriorityLevel.Value)))
                {
                    return _errorManager.ThrowBadRequest<bool>("Debe especificar un nivel de prioridad válido cuando el tipo de solicitud es Requisición.", "ERP:INVALID_PRIORITY");
                }
                else if (purchase.RequestType != PurchaseRequestType.Requisition && request.PriorityLevel.Value != PriorityLevel.None)
                {
                    return _errorManager.ThrowBadRequest<bool>("El nivel de prioridad solo puede especificarse cuando el tipo de solicitud es Requisición.", "ERP:INVALID_PRIORITY");
                }

                historyList.Add(CreateEntry(GetPriorityLabel(purchase.PriorityLevel), GetPriorityLabel(request.PriorityLevel.Value), "Cambio de nivel de prioridad", access.User, workArea));
                purchase.PriorityLevel = request.PriorityLevel.Value;
                hasChanges = true;
            }

            if (request.Observations is not null)
            {
                historyList.Add(CreateEntry(purchase.Concept ?? "", request.Observations, "Actualización de observaciones", access.User, workArea));
                purchase.Concept = request.Observations;
                hasChanges = true;
            }

            if (request.DestinationRequest.HasValue)
            {
                historyList.Add(CreateEntry(purchase.Destination.ToString(), request.DestinationRequest.Value.ToString(), "Cambio de destino de la solicitud", access.User, workArea));
                purchase.Destination = request.DestinationRequest.Value;
                hasChanges = true;
            }

            if (hasChanges)
            {
                purchase.AdditionalData = JsonSerializer.Serialize(historyList, JsonOptions);
            }

            if (request.PurchaseRequestItems != null && request.PurchaseRequestItems.Count > 0)
            {
                var updateResult = await UpdateItemsAsync(purchase, request.PurchaseRequestItems, cancellationToken);
                if (!updateResult)
                {
                    return false;
                }
            }

            await _unitOfWork.PurchaseRequests.UpdateAsync(purchase);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("✅ Solicitud de compra actualizada exitosamente");

            return true;
        }

        private static List<PurchaseRequestAdditionalData> DeserializeHistory(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return [];
            }

            return JsonSerializer.Deserialize<List<PurchaseRequestAdditionalData>>(json, JsonOptions) ?? [];
        }

        private static PurchaseRequestAdditionalData CreateEntry(string oldValue, string newValue, string description, User? user, WorkArea? workArea)
        {
            var entry = new PurchaseRequestAdditionalData
            {
                NewField = newValue,
                OldFields = oldValue,
                Description = description,
                UpdatedAt = DateTime.UtcNow
            };

            if (user != null)
            {
                entry.UserInformation.UserId = user.Id;
                entry.UserInformation.Email = user.Email;
                entry.UserInformation.Fullname = user.Fullname;
                entry.UserInformation.PictureUrl = null;
                entry.UserInformation.UserStatus = user.UserStatus;
            }

            if (workArea != null)
            {
                entry.UserInformation.WorkAreaInformation = new WorkAreaInformation
                {
                    WorkAreaId = workArea.Id,
                    WorkAreaName = workArea.WorkAreaName,
                    Description = workArea.Description,
                    WorkAreaCode = int.TryParse(workArea.WorkAreaCode, out var code) ? code : default
                };
            }

            return entry;
        }

        private async Task<bool> UpdateItemsAsync(PurchaseRequest purchase, List<UpdatePurchaseRequestItem> payloadItems, CancellationToken cancellationToken)
        {
            var existingItems = purchase.PurchaseRequestItems.ToList();

            foreach (var itemPayload in payloadItems)
            {
                if (!itemPayload.Id.HasValue)
                {
                    await CreateItemAsync(purchase.Id, itemPayload, cancellationToken);
                    continue;
                }

                var existing = existingItems.FirstOrDefault(item => item.Id == itemPayload.Id.Value);

                if (existing is null)
                {
                    return _errorManager.ThrowNotFound<bool>("Uno de los productos de la solicitud no existe.", "ERP:PURCHASE_REQUEST_ITEM_NOT_FOUND");
                }

                if (itemPayload.Quantity.HasValue) existing.Quantity = itemPayload.Quantity.Value;
                if (itemPayload.QuantityUnit.HasValue) existing.QuantityUnit = itemPayload.QuantityUnit.Value;
                if (itemPayload.ProductId.HasValue) existing.ProductId = itemPayload.ProductId.Value;
                if (itemPayload.UnitMeasureId.HasValue) existing.UnitMeasureId = itemPayload.UnitMeasureId.Value;

                if (itemPayload.Description != null) existing.Description = itemPayload.Description;
                if (itemPayload.Justification != null) existing.Justification = itemPayload.Justification;

                if (itemPayload.ImagesProductToChanged != null)
                {
                    var itemData = DeserializeItemAdditionalData(existing.AdditionalData);
                    itemData.ImagesProductToChanged = itemPayload.ImagesProductToChanged;
                    existing.AdditionalData = JsonSerializer.Serialize(itemData, JsonOptions);
                }
            }

            return true;
        }

        private async Task CreateItemAsync(Guid purchaseRequestId, UpdatePurchaseRequestItem itemPayload, CancellationToken cancellationToken)
        {
            var newItem = itemPayload.ToPurchaseRequestItemEntity(purchaseRequestId);
            newItem.AdditionalData = await BuildItemAdditionalDataAsync(itemPayload.ImagesProductToChanged, cancellationToken);
            await _unitOfWork.PurchaseRequestItems.RegisterPurchaseRequestItem(newItem);
        }

        private async Task<string?> BuildItemAdditionalDataAsync(List<string>? images, CancellationToken cancellationToken)
        {
            if (images is not { Count: > 0 })
            {
                return null;
            }

            var uploadedUrls = new List<string>(images.Count);

            foreach (var base64Image in images)
            {
                var imageUrl = await _s3StorageService.UploadImageAsync("Compras", "SolicitudesCompras", base64Image, cancellationToken);
                uploadedUrls.Add(imageUrl);
            }

            var additionalData = new PurchaseRequestItemAdditionalData
            {
                ImagesProductToChanged = uploadedUrls
            };

            return JsonSerializer.Serialize(additionalData, JsonOptions);
        }

        private static PurchaseRequestItemAdditionalData DeserializeItemAdditionalData(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new PurchaseRequestItemAdditionalData();
            }

            return JsonSerializer.Deserialize<PurchaseRequestItemAdditionalData>(json, JsonOptions) ?? new PurchaseRequestItemAdditionalData();
        }

        private static string GetPriorityLabel(PriorityLevel level) => level switch
        {
            PriorityLevel.None => "Sin prioridad",
            PriorityLevel.Critical => "Crítica",
            PriorityLevel.Unforeseen => "Imprevista",
            PriorityLevel.Normal => "Normal",
            PriorityLevel.PrintedStationery => "Papelería impresa",
            _ => level.ToString()
        };
    }
}
