using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Helpers.Purchasing.Services
{
    public static class QuotationRecommendations
    {
        public static void Apply(List<PurchaseRequestItemDto>? items)
        {
            if (items is null)
            {
                return;
            }

            foreach (var item in items)
            {
                var activeQuotes = item.Quotations
                    .Where(q => q.IsActive)
                    .ToList();

                Guid? bestWarranty = activeQuotes
                    .Where(q => q.HasGuarantee)
                    .Select(q => new
                    {
                        q.QuotationId,
                        Days = TimePeriodNormalizer.ToDays(q.WarrantyPeriod, q.WarrantyPeriodTimeType)
                    })
                    .Where(x => x.Days.HasValue)
                    .OrderByDescending(x => x.Days)
                    .Select(x => (Guid?)x.QuotationId)
                    .FirstOrDefault();

                Guid? bestDelivery = activeQuotes
                    .Where(q => q.HasDelivery)
                    .Select(q => new
                    {
                        q.QuotationId,
                        Days = TimePeriodNormalizer.ToDays(q.DeliveryTime, q.DeliveryTimeType)
                    })
                    .Where(x => x.Days.HasValue)
                    .OrderBy(x => x.Days)
                    .Select(x => (Guid?)x.QuotationId)
                    .FirstOrDefault();

                item.Recommendations = new QuotationRecommendationDto
                {
                    BestWarrantyQuotationId = bestWarranty,
                    BestDeliveryQuotationId = bestDelivery
                };
            }
        }
    }
}
