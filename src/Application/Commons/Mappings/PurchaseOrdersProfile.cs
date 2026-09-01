using AutoMapper;
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
                .ForPath(dest => dest.SentByUserInformation.WorkAreaInformation, opt => opt.MapFrom(src => src.SentByUser.WorkArea))

                .ForPath(dest => dest.PurchaseRequest, opt => opt.MapFrom(src => src.PurchaseRequest));

            //Detalle.
            CreateMap<PurchaseOrder, PurchaseOrderDetailsDto>()
                .ForMember(dest => dest.PurchaseOrderId, opt => opt.MapFrom(src => src.Id))

                .ForPath(dest => dest.SentByUserInformation, opt => opt.MapFrom(src => src.SentByUser))
                .ForPath(dest => dest.SentByUserInformation.WorkAreaInformation, opt => opt.MapFrom(src => src.SentByUser.WorkArea))

                .ForPath(dest => dest.ReviewerUserInformation, opt => opt.MapFrom(src => src.ReviewedByUser))

                .ForPath(dest => dest.PurchaseRequestDetails, opt => opt.MapFrom(src => src.PurchaseRequest))
                .ForPath(dest => dest.PurchaseRequestDetails.BranchInformation, opt => opt.MapFrom(src => src.PurchaseRequest.Branch))

                .ForPath(dest => dest.PurchaseRequestDetails.CreatorUserInformation, opt => opt.MapFrom(src => src.PurchaseRequest.RegistrationUser))
                .ForPath(dest => dest.PurchaseRequestDetails.CreatorUserInformation.WorkAreaInformation, opt => opt.MapFrom(src => src.PurchaseRequest.RegistrationUser.WorkArea));
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
    }
}
