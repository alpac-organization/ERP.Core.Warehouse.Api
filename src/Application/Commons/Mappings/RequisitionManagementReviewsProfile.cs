using AutoMapper;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Shopping;
using ERP.Core.Warehouse.Api.Application.Features.RequisitionManagementReviews.v1.Dtos;

using Commands = ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{
    public class RequisitionManagementReviewsProfile : Profile
    {
        public RequisitionManagementReviewsProfile()
        {
            CreateMap<RequisitionManagementReview, RequisitionManagementReviewDto>()
                .ForMember(dest => dest.RequisitionManagementReviewId, opt => opt.MapFrom(src => src.Id))
                .ForPath(dest => dest.SentByUserInformation, opt => opt.MapFrom(src => src.SentByUser))
                .ForPath(dest => dest.SentByUserInformation.WorkAreaInformation, opt => opt.MapFrom(src => src.SentByUser.WorkArea))
                .ForPath(dest => dest.PurchaseRequest, opt => opt.MapFrom(src => src.PurchaseRequest));
        }
    }

    public static class RequisitionManagementReviewMapper
    {
        public static RequisitionManagementReview ToRequisitionManagementReviewEntity(this Commands.SendPurchaseRequestToManagementReviewCommand request, Guid sentByUserId, Guid purchaseRequestId)
        {
            return new RequisitionManagementReview()
            {
                Id                = Guid.NewGuid(),
                Status            = ManagementReviewStatus.Pending,
                Comments          = request.Comments,
                PurchaseRequestId = purchaseRequestId,
                SentByUserId      = sentByUserId,
                SentToReviewAt    = DateOnly.FromDateTime(DateTime.Now)
            };
        }
    }
}
