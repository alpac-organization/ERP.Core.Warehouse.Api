using Microsoft.Extensions.Logging;
using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Services;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Handlers
{
    public class RegisterPurchaseRequestHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, ICodeGenerator _codeGenerator, ILogger<RegisterPurchaseRequestHandler> _logger) :  BaseValidatorHandler<RegisterPurchaseRequestCommand, bool>(_unitOfWork, _errorManager)
    {
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

            Guid areaId = access.User.AreaId;

            if (access.Role?.RoleType == RoleType.Administrator && request.AreaId.HasValue)
            {
                areaId = request.AreaId.Value;
            }

            //Generar codigo solicitud
            var (isSucceded, code) = await _codeGenerator.GenerateUniqueCodeToPurchaseRequest(request.RequestType, request.BranchId);

            if (!isSucceded)
            {
                return _errorManager.ThrowBadRequest<bool>("Ocurrio un error la generar la solucitud de comprar", "ERP:ERROR_CODE_GENERATOR");
            }

            var purchaseRequestEntity = PurchaseRequestMapper.ToPurchaseRequestEntity(request, code, areaId);

            await _unitOfWork.PurchaseRequests.RegisterPurchaseRequest(purchaseRequestEntity);

            foreach (var product in request.PurchaseRequestItems)
            {
                //Registrar productos solicitados en la requisición.
                var requestedProductEntity = PurchaseRequestMapper.ToRequestedProductEntity(product, purchaseRequestEntity.Id);
                await _unitOfWork.PurchaseRequestItems.RegisterPurchaseRequestItem(requestedProductEntity);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("✅Se registro exitosame la solicitud de compra");
            return true;
        }
    }
}