using AutoMapper;
using ERP.Core.Warehouse.Api.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Shopping;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{
    public class PurchaseOrdersProfile : Profile
    {
        public PurchaseOrdersProfile()
        {
            //Listado.
            CreateMap<PurchaseOrder, PurchaseOrderDto>()
                .ForMember(dest => dest.PurchaseOrderId, opt => opt.MapFrom(src => src.Id))
                .ForPath(dest => dest.SentByUserInformation, opt => opt.MapFrom(src => src.SentByUser))
                .ForPath(dest => dest.PurchaseRequest, opt => opt.MapFrom(src => src.PurchaseRequest));

            //Detalle.
            CreateMap<PurchaseOrder, PurchaseOrderDetailsDto>()
                .ForMember(dest => dest.PurchaseOrderId, opt => opt.MapFrom(src => src.Id))
                .ForPath(dest => dest.SentByUserInformation, opt => opt.MapFrom(src => src.SentByUser))
                .ForPath(dest => dest.ReviewerUserInformation, opt => opt.MapFrom(src => src.ReviewedByUser))
                .ForPath(dest => dest.PurchaseRequestDetails, opt => opt.MapFrom(src => src.PurchaseRequest));

            //Documento
            CreateMap<PurchaseOrder, PurchaseOrderTemplateDto>()
                .ForPath(dest => dest.Concept, opt => opt.MapFrom(src => src.PurchaseRequest.Concept))
                .ForPath(dest => dest.CompanyInformation, opt => opt.MapFrom(src => src.PurchaseRequest.Branch.Company))
                .ForPath(dest => dest.SentByUserInformation, opt => opt.MapFrom(src => src.PurchaseRequest.RegistrationUser))
            ;
        }
    }

    public static class PurchaseOrdersMapper
    {
        public static PurchaseOrder ToPurchaseOrderEntity(this PurchaseRequestsReviewedManagement review, Guid reviewedByUserId)
        {
            return new PurchaseOrder()
            {
                Id                = Guid.NewGuid(),
                IsActive          = true,
                Comments          = review.Comments,
                SentToReviewAt    = review.SentToReviewAt,
                SentByUserId      = review.SentByUserId,
                ReviewedByUserId  = reviewedByUserId,
                PurchaseRequestId = review.PurchaseRequestId
            };
        }

        public static string GetDocumentTitleByMethodPayment(PaymentMethod paymentMethod)
        {
            return paymentMethod switch
            {
                PaymentMethod.BankTransfer => "Solicitud de transferencia",
                PaymentMethod.Check        => "Solicitud de cheque",
                _                          => "Solicitud de pago"
            };
        }
    }
}   
