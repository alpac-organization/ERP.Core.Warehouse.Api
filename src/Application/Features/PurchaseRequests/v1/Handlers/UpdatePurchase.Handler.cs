using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Database.Domain.Entities.Shopping;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Handlers
{
   public class UpdatePurchaseHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, ILogger<UpdatePurchaseHandler> _logger):BaseValidatorHandler<UpdatePurchaseCommand,bool>(_unitOfWork, _errorManager)
{
   public override async Task<bool> Handle(UpdatePurchaseCommand request, CancellationToken cancellationToken)
   {
     
     var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
      if (!access.IsSuccess)
      {
         return access.ErrorResponse!;
      }

      if(access.Role?.RoleType == RoleType.Supervisor)
      {
         return _errorManager.ThrowBadRequest<bool>("No tienes permiso para realizar esta acción", "ERP:INVALID_ACCESS");
      }

      _logger.LogInformation("🚩 Iniciando Actualizacion de solicitud de compra");

      var purchase = await _unitOfWork.PurchaseRequests.Entities
          .Include(p=>p.PurchaseRequestItems)
          .Where(p=>p.IsActive)
          .Where(p=>p.Id == request.PurchaseRequestId)
          .FirstOrDefaultAsync(cancellationToken);

      if(purchase is null)
      {
         return _errorManager.ThrowNotFound<bool>("La cotización no existe.", "ERP:QUOTATION_NOT_FOUND");
      }

      if (purchase.RequestStatus != PurchaseRequestStatus.Pending)
      {
            return _errorManager.ThrowBadRequest<bool>("Solo se pueden actualizar solicitudes en estado pendiente.",
                    "ERP:PURCHASE_REQUEST_NOT_PENDING"); 
      }

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
          purchase.PriorityLevel = request.PriorityLevel.Value;
      }

         if (request.Observations != null)
            purchase.Concept = request.Observations;

         if (request.DestinationRequest.HasValue)
            purchase.Destination = request.DestinationRequest.Value;

         if(request.PurchaseRequestItems != null && request.PurchaseRequestItems.Count > 0)
         {
            var UpdateResult =  UpdateItemAsync(purchase,request.PurchaseRequestItems);
            if (!UpdateResult)
            {
               return false;
            }
         }
            await _unitOfWork.PurchaseRequests.UpdateAsync(purchase);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("✅ Solicitud de compra actualizada exitosamente");

      return true; 
   }

   private  bool UpdateItemAsync(
      PurchaseRequest purchase,
      List<UpdatePurchaseRequestItem> payloadItems
      )
   {
      var existingItems = purchase.PurchaseRequestItems.ToList(); 

      foreach (var itemPayload in payloadItems)
      {
            if (itemPayload.Id.HasValue)
            {
               var existing = existingItems.FirstOrDefault(item=> item.Id == itemPayload.Id.Value);

               if(existing is null)
               {
                  return _errorManager.ThrowNotFound<bool>("Uno de los productos de la solicitud no existe.",
                            "ERP:PURCHASE_REQUEST_ITEM_NOT_FOUND");
               }

                    if (itemPayload.Quantity.HasValue) existing.Quantity = itemPayload.Quantity.Value;
                    if (itemPayload.QuantityUnit.HasValue) existing.QuantityUnit = itemPayload.QuantityUnit;
                    if (itemPayload.ProductId.HasValue) existing.ProductId = itemPayload.ProductId.Value;
                    if (itemPayload.UnitMeasureId.HasValue) existing.UnitMeasureId = itemPayload.UnitMeasureId.Value;
                    
                    if (itemPayload.Description != null) existing.Description = itemPayload.Description;
                    if (itemPayload.Justification != null) existing.Justification = itemPayload.Justification;
                    if (itemPayload.AdditionalData != null) existing.AdditionalData = itemPayload.AdditionalData;
            } 
      }
         return true;
     }
   }
}