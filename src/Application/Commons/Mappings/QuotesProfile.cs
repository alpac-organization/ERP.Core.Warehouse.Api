using AutoMapper;
using ERP.Core.Database.Domain.Entities.Shopping;
using ERP.Core.Database.Domain.Entities.Warehouse;
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
                .ForMember(dest => dest.Observations,     opt => opt.MapFrom(src => src.Observations))
                .ForMember(dest => dest.QuotationDetails, opt => opt.Ignore());

            CreateMap<QuoteDetail, QuotationDetailsDto>()
                .ForMember(dest => dest.QuotationDetailId,  opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Amount,             opt => opt.MapFrom(src => src.Amount))
                .ForMember(dest => dest.IndividualPrice,    opt => opt.MapFrom(src => src.IndividualPrice))
                .ForMember(dest => dest.Observations,       opt => opt.MapFrom(src => src.Observations))
                .ForMember(dest => dest.AdditionalData,     opt => opt.MapFrom(src => src.AdditionalData))

                .ForPath(dest => dest.SupplierDetails.SupplierId,           opt => opt.MapFrom(src => src.Supplier.Id))
                .ForPath(dest => dest.SupplierDetails.SupplierLegalName,    opt => opt.MapFrom(src => src.Supplier.SuppliersLegalName))
                .ForPath(dest => dest.SupplierDetails.ContactPhoneNumber,   opt => opt.MapFrom(src => src.Supplier.ContactPhoneNumber))
                .ForPath(dest => dest.SupplierDetails.IdentificationNumber, opt => opt.MapFrom(src => src.Supplier.IdentificationNumber))
                .ForPath(dest => dest.SupplierDetails.IdentificationType,   opt => opt.MapFrom(src => src.Supplier.IdentificationType))
                .ForPath(dest => dest.SupplierDetails.ConstitutionType,     opt => opt.MapFrom(src => src.Supplier.ConstitutionType))

                .ForPath(dest => dest.ProductDetails.ProductId,   opt => opt.MapFrom(src => src.Product.Id))
                .ForPath(dest => dest.ProductDetails.ProductName, opt => opt.MapFrom(src => src.Product.ProductName))
                .ForPath(dest => dest.ProductDetails.Description, opt => opt.MapFrom(src => src.Product.Description))
                .ForPath(dest => dest.ProductDetails.UsageType,   opt => opt.MapFrom(src => src.Product.UsageType))

                .ForPath(dest => dest.UnitMeasureDatails.UnitMeasureId, opt => opt.MapFrom(src => src.UnitMeasure.Id))
                .ForPath(dest => dest.UnitMeasureDatails.Code,          opt => opt.MapFrom(src => src.UnitMeasure.Code))
                .ForPath(dest => dest.UnitMeasureDatails.Name,          opt => opt.MapFrom(src => src.UnitMeasure.Name))
                .ForPath(dest => dest.UnitMeasureDatails.Symbol,        opt => opt.MapFrom(src => src.UnitMeasure.Symbol))

                .ForPath(dest => dest.ProductDetails.CategoryDetails.CategoryId,   opt => opt.MapFrom(src => src.Product.Category.Id))
                .ForPath(dest => dest.ProductDetails.CategoryDetails.CategoryName, opt => opt.MapFrom(src => src.Product.Category.Name))
                .ForPath(dest => dest.ProductDetails.CategoryDetails.ParentId,     opt => opt.MapFrom(src => src.Product.Category.ParentId))
                .ForPath(dest => dest.ProductDetails.CategoryDetails.CategoryCode, opt => opt.MapFrom(src => src.Product.Category.Code));

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

        public static Product ToProductEntity(this Commands.ProductInformation command)
        {
            return new()
            {
                Id          = Guid.NewGuid(),
                CategoryId  = command.CategoryId,
                ProductName = command.ProductName,
                Description = command.Description,
                UsageType   = command.UsageType, 
            };
        }

        public static Supplier ToSupplierEntity(this Commands.SupplierDatails command, string registerBy)
        {
            return new()
            {
                Id                   = Guid.NewGuid(),
                IsActive             = true,
                RegisterBy           = registerBy,
                SuppliersLegalName   = command.SupplierName!,
                ConstitutionType     = command.ConstitutionType,
                ContactPhoneNumber   = command.ContactPhoneNumber,
                IdentificationNumber = command.IdentificationNumber,
                IdentificationType   = command.IdentificationType
            };
        }

        public static QuoteDetail ToQuoteDetailsEntity(this Commands.QuoteDetails command)
        {
            return new()
            {
                Id  = Guid.NewGuid(),
                Amount = command.Amount,
                AdditionalData = command.AdditionalData,
            };
        }

    }
}