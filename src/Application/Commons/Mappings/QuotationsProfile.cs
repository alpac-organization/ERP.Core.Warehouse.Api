using AutoMapper;
using ERP.Core.Database.Domain.Entities.Shopping;

using Commands = ERP.Core.Warehouse.Api.Application.Features.Quotations.v1.Commands;
using QuotationDtos = ERP.Core.Warehouse.Api.Application.Features.Quotations.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{

    public class QuotationsProfile : Profile
    {
        public QuotationsProfile()
        {
            CreateMap<Quotation, QuotationDtos.QuotationItems>()
                .ForMember(dest => dest.QuotationId, opt => opt.MapFrom(src => src.Id));

            CreateMap<Quotation, QuotationDtos.QuotationDto>()
                .ForMember(dest => dest.QuotationId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.SupplierInformation, opt => opt.MapFrom(src => src.Supplier));

            CreateMap<Supplier, QuotationDtos.SupplierInformationDto>();
        }
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