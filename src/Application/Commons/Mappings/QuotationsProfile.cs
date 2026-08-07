using AutoMapper;
using ERP.Core.Database.Domain.Entities.Shopping;

using Commands = ERP.Core.Warehouse.Api.Application.Features.Quotations.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{

    public class QuotationsProfile : Profile
    {
        
    }

    public static class QuotationsMapper
    {
        public static Quotation ToQuotationsEntity(this Commands.QuotationItem command)
        {
            return new()
            {
                Id                     = Guid.NewGuid(),
                IsActive               = true,
                QuoteDate              = DateOnly.FromDateTime(DateTime.Now),
                HasDelivery            = command.HasDelivery,
                HasGuarantee           = command.HasGuarantee,
                BrandProduct           = command.BrandProduct,
                DeliveryTime           = command.DeliveryTime,
                DeliveryTimeType       = command.DeliveryTimeType,
                SupplierId             = command.SupplierId,
                Price                  = command.Price,
                PurchaseRequestItemId  = command.PurchaseRequestItemId,
                WarrantyPeriodTimeType = command.WarrantyPeriodTimeType,
                WarrantyPeriod         = command.WarrantyPeriod,
                Iva                    = command?.Iva ?? 0.0m,
                PriceUnit              = command?.PriceUnit ?? 0.0m,
            };
        }
    }
}