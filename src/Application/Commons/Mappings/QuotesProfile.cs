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

            CreateMap<QuoteDetail, QuotationDetailsDto>()
                .ForMember(dest => dest.QuotationDetailId,  opt => opt.MapFrom(src => src.Id));
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