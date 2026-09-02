using System.Globalization;
using AutoMapper;
using ERP.Core.Database.Domain.Entities.Shopping;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Domain.Entities.ObjectValues;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{
    public class PurchaseOrderDocumentProfile : Profile
    {
        public PurchaseOrderDocumentProfile()
        {
            CreateMap<PurchaseOrder, PurchaseOrderDocumentTemplateDto>()
                // Concepto
                .ForMember(dest => dest.Concept, opt => opt.MapFrom(src => src.PurchaseRequest.Concept))

                // Empresa / encabezado
                .ForMember(dest => dest.CompanyInformation, opt => opt.MapFrom(src => new CompanyInformation
                {
                    Ruc           = src.PurchaseRequest.Branch.Company.Ruc,
                    CompanyName   = src.PurchaseRequest.Branch.Company.CompanieName,
                    CompanyAlias  = src.PurchaseRequest.Branch.Company.Code,
                    CompanyLogoUrl = src.PurchaseRequest.Branch.Company.ImageUrl
                }))

                // Usuario registrante
                .ForMember(dest => dest.RegisteredByUser, opt => opt.MapFrom(src => new UserInformation
                {
                    UserName  = src.PurchaseRequest.RegistrationUser.Fullname,
                    UserEmail = src.PurchaseRequest.RegistrationUser.Email
                }))

                // Información general del documento
                .ForMember(dest => dest.DocumentInfo, opt => opt.MapFrom(src => new DocumentInfo
                {
                    RequestCode      = src.PurchaseRequest.Code,
                    ReferenceNumber  = src.Id.ToString(),
                    Date             = src.SentToReviewAt.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                    QuoteCount       = AcceptedQuotations(src).Count(),
                    AssignmentNumber = null
                }))

                // Información de pago
                .ForMember(dest => dest.PaymentInfo, opt => opt.MapFrom(src => new PaymentInfo
                {
                    Payee              = AcceptedQuotations(src).Select(q => q.Supplier.SuppliersLegalName).FirstOrDefault(),
                    Customer           = src.PurchaseRequest.Branch.Company.Alias,
                    Department         = src.PurchaseRequest.WorkArea.WorkAreaName,
                    ServiceAmount      = AcceptedQuotations(src).Sum(q => q.PriceTotal),
                    Vat                = AcceptedQuotations(src).Sum(q => q.Iva),
                    NetToPay           = AcceptedQuotations(src).Sum(q => q.PriceTotal + q.Iva),
                    ExemptServiceAmount = 0m,
                    OtherDisbursement  = 0m,
                    IncomeTax          = 0m,
                    MunicipalTax       = 0m,
                    Others             = 0m
                }))

                // Firmas
                .ForMember(dest => dest.Signatures, opt => opt.MapFrom(src => new DocumentSignatureInfo
                {
                    RequestedBy = src.PurchaseRequest.RegistrationUser.Fullname,
                    ApprovedBy  = src.ReviewedByUser == null ? null : src.ReviewedByUser.Fullname
                }));
        }

        private static IEnumerable<Quotation> AcceptedQuotations(PurchaseOrder src)
        {
            if (src.PurchaseRequest?.PurchaseRequestItems == null)
                return Enumerable.Empty<Quotation>();

            return src.PurchaseRequest.PurchaseRequestItems
                .Where(item => item.Quotations != null)
                .SelectMany(item => item.Quotations)
                .Where(q => q.IsActive && q.IsAcceptedForPurchase)
                .ToList();
        }
    }
}
