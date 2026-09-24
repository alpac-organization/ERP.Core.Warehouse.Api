using System;
using System.Threading;
using System.Threading.Tasks;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Database.Domain.Entities.Shopping;
using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Commons.Helpers
{
    public static class RequisitionAnnulmentHelper
    {
        public static async Task AnnulItemsAndQuotationsAsync(
            IUnitOfWork unitOfWork,
            PurchaseRequest purchaseRequest,
            DateTime now)
        {
            foreach (var item in purchaseRequest.PurchaseRequestItems)
            {
                item.HasQuotation = false;
                await unitOfWork.PurchaseRequestItems.UpdateAsync(item);

                foreach (var quote in item.Quotations)
                {
                    quote.IsActive = false;
                    quote.IsAcceptedForPurchase = false;
                    quote.DeletedAt = now;
                    await unitOfWork.Quotations.UpdateAsync(quote);
                }
            }
        }

        public static void ApplyAnnulmentToPurchaseRequest(
            PurchaseRequest purchaseRequest,
            bool isQuotationOnly,
            string? reason,
            Guid userId,
            DateTime now)
        {
            purchaseRequest.RequestStatus = isQuotationOnly ? PurchaseRequestStatus.Approved : PurchaseRequestStatus.Rejected;
            purchaseRequest.AnnulmentReason = reason;
            purchaseRequest.AnnulledByUserId = userId;
            purchaseRequest.DeletedAt = isQuotationOnly ? null : now;
        }
    }
}
