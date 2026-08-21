using System.Text.Json;
using Microsoft.Extensions.Logging;

using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Shopping;

using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Services;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Commands;

using ERP.Core.Application.Commons.Interfaces.AWS;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Handlers
{
    public class RegisterPurchaseRequestHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, ICodeGenerator _codeGenerator, IS3StorageService _s3StorageService,
        ILogger<RegisterPurchaseRequestHandler> _logger
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

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("✅Se registro exitosame la solicitud de compra");
            return true;
        }
    }
}