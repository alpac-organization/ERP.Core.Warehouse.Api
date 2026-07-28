using AutoMapper;
using ERP.Core.Database.Domain.Entities.Shopping;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.Quotes.v1.Dtos;

using Commands = ERP.Core.Warehouse.Api.Application.Features.Quotes.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{

    public class QuotesProfile : Profile
    {
        public QuotesProfile()
        {
            CreateMap<Quotation, QuotationDto>()
                .ForMember(dest => dest.QuotationId,      opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.MadeBy,           opt => opt.MapFrom(src => src.MadeBy))
                .ForMember(dest => dest.QuoteDate,        opt => opt.MapFrom(src => src.QuoteDate))
                .ForMember(dest => dest.QuotationCode,    opt => opt.MapFrom(src => src.QuotationCode))
                .ForMember(dest => dest.BranchName,       opt => opt.MapFrom(src => src.Branch != null ? src.Branch.BranchName : null))
                .ForMember(dest => dest.Observations,     opt => opt.MapFrom(src => src.Observations));

            CreateMap<Quotation, QuotationInformationDto>()
                .IncludeBase<Quotation, QuotationDto>()
                .ForMember(dest => dest.QuotedSuppliers, opt => opt.Ignore());


            CreateMap<QuoteDetail, QuotationDetailsDto>()
                .ForMember(dest => dest.QuotationDetailId,  opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Status,  opt => opt.MapFrom(src => src.Status))

                .ForPath(dest => dest.SupplierInformation.SupplierId,           opt => opt.MapFrom(src => src.Supplier.Id))
                .ForPath(dest => dest.SupplierInformation.ConstitutionType,     opt => opt.MapFrom(src => src.Supplier.ConstitutionType))
                .ForPath(dest => dest.SupplierInformation.SupplierLegalName,    opt => opt.MapFrom(src => src.Supplier.SuppliersLegalName))
                .ForPath(dest => dest.SupplierInformation.IdentificationType,   opt => opt.MapFrom(src => src.Supplier.IdentificationType))
                .ForPath(dest => dest.SupplierInformation.IdentificationNumber, opt => opt.MapFrom(src => src.Supplier.IdentificationNumber))

                .ForPath(dest => dest.SupplierInformation.SupplierDetails.Address,            opt => opt.MapFrom(src => src.Supplier.SupplierDetails.Address))
                .ForPath(dest => dest.SupplierInformation.SupplierDetails.ContactName,        opt => opt.MapFrom(src => src.Supplier.SupplierDetails.ContactName))
                .ForPath(dest => dest.SupplierInformation.SupplierDetails.ContactEmail,       opt => opt.MapFrom(src => src.Supplier.SupplierDetails.ContactEmail))
                .ForPath(dest => dest.SupplierInformation.SupplierDetails.ContactPhoneNumber, opt => opt.MapFrom(src => src.Supplier.SupplierDetails.ContactPhoneNumber))
                .ForPath(dest => dest.SupplierInformation.SupplierDetails.CreditDays,         opt => opt.MapFrom(src => src.Supplier.SupplierDetails.CreditDays))
                .ForPath(dest => dest.SupplierInformation.SupplierDetails.HasCredit,          opt => opt.MapFrom(src => src.Supplier.SupplierDetails.HasCredit));

            CreateMap<QuotedProduct, QuotedProductDto>()
                .ForMember(dest => dest.QuotedProductId,    opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Quantity,           opt => opt.MapFrom(src => src.Quantity))
                .ForMember(dest => dest.PricePerUnit,       opt => opt.MapFrom(src => src.PricePerUnit))
                .ForMember(dest => dest.AdditionalData,     opt => opt.MapFrom(src => src.AdditionalData))
                .ForMember(dest => dest.PriceWholesale,     opt => opt.MapFrom(src => src.PriceWholesale))
                .ForMember(dest => dest.EquivalentQuantity, opt => opt.MapFrom(src => src.EquivalentQuantity));
            
            ;
                // .ForPath(dest => dest.ProductDetails.CategoryDetails.CategoryId,   opt => opt.MapFrom(src => src.Product.Category.Id))
                // .ForPath(dest => dest.ProductDetails.CategoryDetails.CategoryName, opt => opt.MapFrom(src => src.Product.Category.Name))
                // .ForPath(dest => dest.ProductDetails.CategoryDetails.ParentId,     opt => opt.MapFrom(src => src.Product.Category.ParentId))
                // .ForPath(dest => dest.ProductDetails.CategoryDetails.CategoryCode, opt => opt.MapFrom(src => src.Product.Category.Code));            
        }
    }
    
    public static class QuotationMapper
    {
        public static Quotation ToQuotationEntity(this Commands.RegisterQuoteCommand command, string MadeBy, string quotationCode)
        {
            return new()
            {
                Id            = Guid.NewGuid(),
                MadeBy        = MadeBy,
                QuotationCode = quotationCode,
                BranchId      = command.BranchId,
                QuoteDate     = command.QuoteDate,
                Observations  = command.Observations,
            };
        }

        public static QuoteDetail ToQuotationDetailEntity(this Commands.QuoteDetails command, Guid quotationId)
        {
            return new()
            {
                Id            = Guid.NewGuid(),
                QuotationId   = quotationId,
                SupplierId    = command.SupplierId,
                Status        = QuotationStatus.Pending,
                ApproximateTotalCost = 0.0m
            };
        }
    }
}