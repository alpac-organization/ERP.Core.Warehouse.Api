using AutoMapper;
using ERP.Core.Database.Domain.Entities.Shopping;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos;

using Commands = ERP.Core.Warehouse.Api.Application.Features.Quotations.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{
    public class QuotationsProfile : Profile
    {
        public QuotationsProfile()
        {
            CreateMap<Quotation, QuotationInformationDto>()
                .ForMember(dest => dest.QuotationId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.IsBestOption, opt => opt.MapFrom(src => EvaluateBestOption(src)))
                .ForPath(dest => dest.SupplierInformation, opt => opt.MapFrom(src => src.Supplier));
        }

        private static bool EvaluateBestOption(Quotation quotation)
        {
            // Lógica simple de evaluación: tiene delivery, tiene garantía y es de buena calidad
            return quotation.HasDelivery && quotation.HasGuarantee && quotation.ProductQuality == ProductQuality.Excellent;
        }
    }

    public static class QuotationsMapper
    {
        public static Quotation ToQuotationsEntity(
            this Commands.QuotationItem command,
            decimal priceUnit,
            decimal priceTotal,
            decimal ivaAmount,
            Guid? supplierProductId,
            string? additionalData)
        {
            return new()
            {
                Id = Guid.NewGuid(),
                IsActive = true,
                QuoteDate = DateOnly.FromDateTime(DateTime.UtcNow),
                HasDelivery = command.HasDelivery,
                HasGuarantee = command.HasGuarantee,
                InventoryAvailable = command.InventoryAvailable,
                BrandProduct = command.BrandProduct,
                DeliveryTime = command.DeliveryTime,
                DeliveryTimeType = command.DeliveryTimeType,
                SupplierId = command.SupplierId,
                SupplierSelectionJustification = command.SupplierSelectionJustification,
                PurchaseRequestItemId = command.PurchaseRequestItemId,
                WarrantyPeriodTimeType = command.WarrantyPeriodTimeType,
                WarrantyPeriod = command.WarrantyPeriod,
                AvailabilityTime = command.AvailabilityTime,
                AvailabilityTimeType = command.AvailabilityTimeType,
                PaymentMethodType = command.PaymentMethodType,
                ProductQuality = command.ProductQuality,
                PriceUnit = priceUnit,
                PriceTotal = priceTotal,
                Iva = ivaAmount,
                SupplierProductId = supplierProductId,
                AdditionalData = additionalData
            };
        }
    }
}
