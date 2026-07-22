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
                .ForMember(dest => dest.QuotationId, opt => opt.MapFrom(src => src.Id));
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
                SuppliersLegalName   = command.SupplierName,
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