using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Application.Commons.Interfaces.AWS;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Shopping;

using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Services;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Commons.Options;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Handlers
{
    public class RegisterPurchaseRequestHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, ICodeGenerator _codeGenerator, IS3StorageService _s3StorageService,
        ILogger<RegisterPurchaseRequestHandler> _logger,
        ISimpleNotificationServices _simpleNotificationServices,
        IOptions<PurchaseRequestOptions> _options
    ): BaseValidatorHandler<RegisterPurchaseRequestCommand, bool>(_unitOfWork, _errorManager)
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        };



        public override async Task<bool> Handle(RegisterPurchaseRequestCommand request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse;
            }

            if (access.Role?.RoleType == RoleType.Supervisor)
            {
                return _errorManager.ThrowBadRequest<bool>("No tienes permiso para realizar esta acción", "ERP:INVALID_ACCESS");
            }

            foreach(var purchaseRequest in request.PurchaseRequests)
            {
                Guid areaId = access.User.AreaId;

                if (access.Role?.RoleType == RoleType.Administrator && purchaseRequest.AreaId.HasValue)
                {
                    areaId = purchaseRequest.AreaId.Value;
                }

                var (isSucceded, code) = await _codeGenerator.GenerateUniqueCodeToPurchaseRequest(purchaseRequest.RequestType, purchaseRequest.BranchId);

                if (!isSucceded)
                {
                    return _errorManager.ThrowBadRequest<bool>("Ocurrio un error la generar la solucitud de comprar", "ERP:ERROR_CODE_GENERATOR");
                }

                var purchaseRequestEntity = PurchaseRequestMapper.ToPurchaseRequestEntity(purchaseRequest, code, areaId, access.User.Id);
                await _unitOfWork.PurchaseRequests.RegisterPurchaseRequest(purchaseRequestEntity);

                // Guardame imagenes en el S3 Bucket
                foreach (var product in purchaseRequest.PurchaseRequestItems)
                {
                    var purchaseRequestItemEntity = PurchaseRequestMapper.ToPurchaseRequestItemEntity(product, purchaseRequestEntity.Id);

                    if (!string.IsNullOrWhiteSpace(purchaseRequestItemEntity.AdditionalData))
                    {
                        var additionalData = JsonSerializer.Deserialize<PurchaseRequestItemAdditionalData>(purchaseRequestItemEntity.AdditionalData, JsonOptions);

                        if (additionalData?.ImagesProductToChanged is { Count: > 0 })
                        {
                            var uploadedUrls = new List<string>();

                            foreach (var base64Image in additionalData.ImagesProductToChanged)
                            {
                                var imageUrl = await _s3StorageService.UploadImageAsync("Compras", "SolicitudesCompras", base64Image, cancellationToken);
                                uploadedUrls.Add(imageUrl);
                            }
                            
                            additionalData.ImagesProductToChanged = uploadedUrls;
                            purchaseRequestItemEntity.AdditionalData = JsonSerializer.Serialize(additionalData, JsonOptions);
                        }
                    }
                    
                    await _unitOfWork.PurchaseRequestItems.RegisterPurchaseRequestItem(purchaseRequestItemEntity);
                }
            }

            #region  Enviar push notification 
            var notificationConfig = _options.Value;

            string userName = access.User?.Fullname ?? "Un usuario";
            string descriptionCopy = (notificationConfig.Description ?? "{{Name}} registró una nueva solicitud de compra {{Type}}.")
                .Replace("{{Name}}", userName);

            var targetProfiles = await _unitOfWork.Profiles.Entities
                .Include(p => p.UserModuleRole)
                    .ThenInclude(umr => umr.Role)
                .Where(p => p.IsActive)
                .Where(p => p.UserId != request.UserId)
                .Where(p => p.CompanyId == request.CompanyId)
                .Where(p => p.UserModuleRole.Any(
                        umr => umr.ModuleCode == request.ModuleCode && (
                            umr.Role.RoleType == RoleType.Administrator || 
                            umr.Role.RoleType == RoleType.Manager
                        )
                    )
                )
                .ToListAsync(cancellationToken);

            var targetProfileIds = targetProfiles.Select(p => p.Id).ToList();

            // Crear registros internos de notificación para cada destinatario
            foreach (var profile in targetProfiles)
            {
                //Lo registramos en su bandeja
                await _unitOfWork.Notifications.CreateNotification(new()
                {
                    Title          = notificationConfig.Title,
                    Description    = descriptionCopy ,                       
                    PathRedirect   = "/purchasing",
                    AdditionalData = JsonSerializer.Serialize("{}"),
                    UserId         = profile.UserId,
                });
            }

            // Obtener sus dispositivos y enviar notificaciones a ellos en especifico.
            var devices = await _unitOfWork.Devices.Entities
                .Where(device => device.IsActive)
                .Where(device => targetProfileIds.Contains(device.UserProfileId))
                .ToListAsync(cancellationToken);
            
            //Mapear todos los dispositivos.
            foreach (var device in devices)
            {
                var result = await _simpleNotificationServices.SendPushNotificationAsync(device.EndpointArn ?? "", new()
                {
                    Title    = notificationConfig.Title,
                    Body     = descriptionCopy,
                    WebPushConfig = new()
                    {
                        Badge = access.Profile.Company.ImageUrl,
                        Icon  = access.Profile.Company.ImageUrl
                    }
                });

                _logger.LogInformation("Push notification result for device {EndpointArn}: {@Result}", device.EndpointArn, result);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("✅Se registro exitosame la solicitud de compra");

            #endregion

            return true;
        }
    }
}