using MediatR;
using System.Text.Json.Serialization;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.Quotations.v1.Commands
{
    /// <summary>
    /// Actualiza una cotización existente; permite cambiar proveedor creando vínculo si falta.
    /// </summary>
    public class UpdateQuotationCommand : BaseRequest, IRequest<bool>
    {
        /// <summary>
        /// Identificador de la cotización.
        /// </summary>
        [JsonIgnore]
        public Guid QuotationId { get; set; }

        /// <summary>
        /// Nuevo proveedor (opcional).
        /// </summary>
        public Guid? SupplierId { get; set; }

        public bool? HasDelivery { get; set; }
        public bool? HasGuarantee { get; set; }
        public bool? InventoryAvailable { get; set; }

        public string? BrandProduct { get; set; }
        public ProductQuality? ProductQuality { get; set; }
        public PaymentMethodType? PaymentMethodType { get; set; }

        public decimal? DeliveryTime { get; set; }
        public decimal? WarrantyPeriod { get; set; }
        public TimeType? DeliveryTimeType { get; set; }
        public TimeType? WarrantyPeriodTimeType { get; set; }

        public decimal? AvailabilityTime { get; set; }
        public TimeType? AvailabilityTimeType { get; set; }

        public string? SupplierSelectionJustification { get; set; }

        /// <summary>
        /// Precio unitario para crear vínculo o como fallback de precio.
        /// </summary>
        public decimal? PriceUnit { get; set; }

        /// <summary>
        /// Precio preferencial al crear el vínculo.
        /// </summary>
        public decimal? PreferentialPrice { get; set; }

        /// <summary>
        /// Moneda del vínculo al crearlo.
        /// </summary>
        public Currency? Currency { get; set; }

        /// <summary>
        /// Si es <c>true</c> (default), crea el vínculo cuando se cambia a un proveedor no vinculado.
        /// </summary>
        public bool CreateSupplierProductIfMissing { get; set; } = true;

        public QuotationAttachmentsInput? Attachments { get; set; }
    }
}
