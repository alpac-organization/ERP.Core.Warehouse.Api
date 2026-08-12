using AutoMapper;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Shopping;
using ERP.Core.Warehouse.Api.Application.Features.RequisitionAccountingReviews.v1.Dtos;

using Commands = ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{
    public class RequisitionAccountingReviewsProfile : Profile
    {
        public RequisitionAccountingReviewsProfile()
        {
            CreateMap<RequisitionAccountingReview, RequisitionAccountingReviewDto>()
                .ForMember(dest => dest.RequisitionAccountingReviewId, opt => opt.MapFrom(src => src.Id))
                .ForPath(dest => dest.SentByUserInformation,           opt => opt.MapFrom(src => src.SentByUser))
                .ForPath(dest => dest.SentByUserInformation.WorkAreaInformation, opt => opt.MapFrom(src => src.SentByUser.WorkArea));

            CreateMap<RequisitionAccountingReview, RequisitionAccountingReviewDetailsDto>()
                .ForMember(dest => dest.RequisitionAccountingReviewId, opt => opt.MapFrom(src => src.Id))

                .ForPath(dest => dest.PurchaseRequest,       opt => opt.MapFrom(src => src.PurchaseRequest))
                .ForPath(dest => dest.PurchaseRequest.InformationFromRequestingArea,       opt => opt.MapFrom(src => src.PurchaseRequest.WorkArea))
                .ForPath(dest => dest.PurchaseRequest.InformationFromRequestingArea.CostCenters,       opt => opt.MapFrom(src => src.PurchaseRequest.WorkArea.CostCenters))
                .ForPath(dest => dest.SentByUserInformation, opt => opt.MapFrom(src => src.SentByUser))
                .ForPath(dest => dest.SentByUserInformation.WorkAreaInformation, opt => opt.MapFrom(src => src.SentByUser.WorkArea))

                .ForPath(dest => dest.PurchaseRequest.RequestedProducts,       opt => opt.MapFrom(src => src.PurchaseRequest.PurchaseRequestItems));

            // CreateMap<Quotation, QuotationInformationDto>()
            //     .ForMember(dest => dest.QuotationId, opt => opt.MapFrom(src => src.Id))

            //     .ForPath(dest => dest.SupplierInformation.SupplierId, opt => opt.MapFrom(src => src.Supplier.Id))
            //     .ForPath(dest => dest.SupplierInformation.ImageUrl, opt => opt.MapFrom(src => src.Supplier.ImageUrl))
            //     .ForPath(dest => dest.SupplierInformation.SuppliersLegalName, opt => opt.MapFrom(src => src.Supplier.SuppliersLegalName))
            //     .ForPath(dest => dest.SupplierInformation.IdentificationNumber, opt => opt.MapFrom(src => src.Supplier.IdentificationNumber))
            //     .ForPath(dest => dest.SupplierInformation.IdentificationType, opt => opt.MapFrom(src => src.Supplier.IdentificationType));
        }
    }

    public static class RequisitionAccountingReviewMapper
    {
        public static RequisitionAccountingReview ToRequisitionAccountingReviewEntity(this Commands.SendPurchaseRequestToReviewCommand request, Guid sentByUserId)
        {
            return new RequisitionAccountingReview()
            {
                Id                = Guid.NewGuid(),
                Status            = AccountingReviewStatus.Pending,
                Comments          = request.Comments,
                PurchaseRequestId = request.PurchaseRequestId,
                SentByUserId      = sentByUserId,
                SentToReviewAt    = DateOnly.FromDateTime(DateTime.Now)
            };
        }
    }
}
