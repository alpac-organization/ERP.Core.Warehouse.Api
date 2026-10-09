using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Application.Commons.Interfaces.AWS;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Shopping;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Commons.Options;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Handlers
{
    public class ProcessPurchaseRequestHandler(
        IUnitOfWork _unitOfWork,
        IErrorManager _errorManager,
        ISimpleNotificationServices _simpleNotificationServices,
        IOptions<Dictionary<PurchaseRequestStatus, ProcessPurchaseRequestOptions>> _options)
        : BaseValidatorHandler<ProcessPurchaseRequestCommand, bool>(_unitOfWork, _errorManager)
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        };

        private static readonly ProcessPurchaseRequestOptions DefaultCopy = new()
        {
            Title = "Actualización de Solicitud",
            Description = "Se actualizó el estado de la solicitud de compra.",
            Icon = "📦"
        };

        private ProcessPurchaseRequestOptions GetCopy(PurchaseRequestStatus status)
        {
            if (_options.Value.TryGetValue(status, out var copy))
            {
                return copy;
            }

            return DefaultCopy;
        }

        public override async Task<bool> Handle(ProcessPurchaseRequestCommand request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse;
            }

            if (access.Role?.RoleType == RoleType.Supervisor || access.Role?.RoleType == RoleType.Operator)
            {
                return _errorManager.ThrowBadRequest<bool>("No tienes permiso para realizar esta acción", "ERP:INVALID_ACCESS");
            }

            var purchaseRequest = await _unitOfWork.PurchaseRequests.Entities
                .Where(x => x.Id == request.PurchaseRequestId)
                .Where(x => x.RequestStatus == PurchaseRequestStatus.Pending)
                .FirstOrDefaultAsync(cancellationToken);

            if (purchaseRequest == null)
            {
                return _errorManager.ThrowNotFound<bool>("La solicitud de compra no fue encontrada o no está en estado pendiente", "ERP:PURCHASE_REQUEST_NOT_FOUND");
            }

            switch (request.NewStatus)
            {
                case PurchaseRequestStatus.Approved:
                {
                    purchaseRequest.UserRevisionId = access.User.Id;
                    purchaseRequest.RequestStatus = request.NewStatus;
                    purchaseRequest.RevisionDate = DateOnly.FromDateTime(DateTime.UtcNow);

                    await _unitOfWork.PurchaseRequests.UpdateAsync(purchaseRequest);
                    break;
                }
                case PurchaseRequestStatus.Rejected:
                {
                    var rejectionReason = await _unitOfWork.SubCatalogs.Entities
                        .Where(sub => sub.Id == request.ReasonRejectionId)
                        .Where(sub => sub.CatalogId == (int)CatalogType.PurchaseRejectionReasons)
                        .Where(sub => sub.IsActive)
                        .Where(sub => sub.DeletedAt == null)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (rejectionReason is null)
                    {
                        return _errorManager.ThrowBadRequest<bool>(
                            "El motivo de rechazo no es válido o no pertenece al catálogo de motivos de rechazo",
                            "ERP:INVALID_REJECTION_REASON");
                    }

                    purchaseRequest.UserRevisionId = access.User.Id;
                    purchaseRequest.RequestStatus = request.NewStatus;
                    purchaseRequest.RevisionDate = DateOnly.FromDateTime(DateTime.UtcNow);
                    purchaseRequest.ReasonRejectionId = rejectionReason.Id;
                    purchaseRequest.RejectionComments = string.IsNullOrWhiteSpace(request.RejectionComments)
                        ? null
                        : request.RejectionComments.Trim();

                    var historyList = string.IsNullOrWhiteSpace(purchaseRequest.AdditionalData)
                        ? []
                        : JsonSerializer.Deserialize<List<PurchaseRequestAdditionalData>>(purchaseRequest.AdditionalData, JsonOptions) ?? [];

                    var entry = new PurchaseRequestAdditionalData
                    {
                        NewField = rejectionReason.CatalogName ?? string.Empty,
                        OldFields = purchaseRequest.RejectionComments ?? string.Empty,
                        Description = "Rechazo de solicitud",
                        UpdatedAt = DateTime.UtcNow
                    };

                    if (access.User != null)
                    {
                        entry.UserInformation.UserId = access.User.Id;
                        entry.UserInformation.Email = access.User.Email;
                        entry.UserInformation.Fullname = access.User.Fullname;
                        entry.UserInformation.UserStatus = access.User.UserStatus;
                    }

                    historyList.Add(entry);
                    purchaseRequest.AdditionalData = JsonSerializer.Serialize(historyList, JsonOptions);

                    await _unitOfWork.PurchaseRequests.UpdateAsync(purchaseRequest);
                    break;
                }
                case PurchaseRequestStatus.Canceled:
                {
                    purchaseRequest.UserRevisionId = access.User.Id;
                    purchaseRequest.RequestStatus = request.NewStatus;
                    purchaseRequest.RevisionDate = DateOnly.FromDateTime(DateTime.UtcNow);
                    await _unitOfWork.PurchaseRequests.UpdateAsync(purchaseRequest);
                    break;
                }
                default:
                    return _errorManager.ThrowBadRequest<bool>("El nuevo estado de la solicitud no es válido", "ERP:INVALID_STATUS_CHANGE");
            }

            #region Construcción del Copy de Notificación

            var rawCopy = GetCopy(request.NewStatus);
            var reviewerName = access.User?.Fullname ?? "unknow user";

            var title = rawCopy.Title;
            var description = rawCopy.Description?
                .Replace("{{Code}}", purchaseRequest.Code)
                .Replace("{{Name}}", reviewerName);

            #endregion

            //Enviar notificación de confirmación 🔔
            await _unitOfWork.Notifications.CreateNotification(new ()
            {
                Title        = title,
                Description  = description,     
                PathRedirect = "/purchasing",
                UserId       = purchaseRequest.RegisteredByUserId,
                AdditionalData = null,  
            });

            var profile = await _unitOfWork.Profiles.Entities
                .Where(profile => profile.IsActive)
                .Where(profile => profile.CompanyId == request.CompanyId)
                .Where(profile => profile.UserId == purchaseRequest.RegisteredByUserId)
                .FirstOrDefaultAsync(cancellationToken);

            if (profile is not null)
            {
               var devices = await _unitOfWork.Devices.Entities
                    .Where(device => device.IsActive)
                    .Where(device => device.UserProfileId == profile.Id)  // ✅ Id del perfil, no del usuario
                    .ToListAsync(cancellationToken);

                foreach (var device in devices)
                {
                    //Lanzamos las push
                    var result = await _simpleNotificationServices.SendPushNotificationAsync(device.EndpointArn ?? "", new()
                    {
                        Title    = title,
                        Body     = description,
                        WebPushConfig = new()
                        {
                            Badge = "",
                            Icon  = ""
                        }
                    });
                }            
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
