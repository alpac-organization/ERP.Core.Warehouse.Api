using MediatR;
using System.Text.Json.Serialization;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.Quotations.v1.Commands
{
    public class RegisterQuotationCommand : BaseRequest, IRequest<bool>
    {
        public required List<QuotationItem> QuotationItems { get; set; }
    }

    public class QuotationItem
    {
        public Guid SupplierId { get; set; }
        public Guid PurchaseRequestItemId { get; set; }

        public bool HasDelivery { get; set; }
        public bool HasGuarantee { get; set; }

        public decimal Price { get; set; }
        public decimal PriceTotal { get; set; }

        public decimal? Iva { get; set; }
        public decimal? PriceUnit { get; set; }

        public string? BrandProduct { get; set; }

        public decimal? DeliveryTime { get; set; }
        public TimeType? DeliveryTimeType { get; set; }

        public decimal? WarrantyPeriod { get; set; }
        public TimeType? WarrantyPeriodTimeType { get; set; }
    }
}