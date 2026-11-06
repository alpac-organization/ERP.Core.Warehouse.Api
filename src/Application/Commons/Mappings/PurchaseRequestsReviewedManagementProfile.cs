using AutoMapper;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Shopping;

using ERP.Core.Warehouse.Api.Application.Features.RequisitionManagementReviews.v1.Dtos;
using Commands = ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{
    public class PurchaseRequestsReviewedManagementProfile : Profile
    {
        public PurchaseRequestsReviewedManagementProfile()
        {
            Guid profileCompanyId = default;

            CreateMap<PurchaseRequestsReviewedManagement, PurchaseRequestsReviewedManagementDto>()
                .ForMember(dest => dest.PurchaseRequestsReviewedManagementId, opt => opt.MapFrom(src => src.Id))
                .ForPath(dest => dest.SentByUserInformation.UserId, opt => opt.MapFrom(src => src.SentByUser.Id))
                .ForPath(dest => dest.SentByUserInformation.Email, opt => opt.MapFrom(src => src.SentByUser.Email))
                .ForPath(dest => dest.SentByUserInformation.Fullname, opt => opt.MapFrom(src => src.SentByUser.Fullname))
                .ForPath(dest => dest.SentByUserInformation.UserStatus, opt => opt.MapFrom(src => src.SentByUser.UserStatus))
                .ForPath(dest => dest.SentByUserInformation.WorkAreaInformation, opt => opt.MapFrom(src =>
                    src.SentByUser.Profiles
                        .Where(profile => profile.CompanyId == profileCompanyId && profile.WorkArea != null)
                        .Select(profile => profile.WorkArea)
                        .FirstOrDefault()))
                .ForPath(dest => dest.PurchaseRequest.PurchaseRequestId, opt => opt.MapFrom(src => src.PurchaseRequest.Id))
                .ForPath(dest => dest.PurchaseRequest.Code, opt => opt.MapFrom(src => src.PurchaseRequest.Code))
                .ForPath(dest => dest.PurchaseRequest.RequestDate, opt => opt.MapFrom(src => src.PurchaseRequest.RequestDate))
                .ForPath(dest => dest.PurchaseRequest.RevisionDate, opt => opt.MapFrom(src => src.PurchaseRequest.RevisionDate))
                .ForPath(dest => dest.PurchaseRequest.PriorityLevel, opt => opt.MapFrom(src => src.PurchaseRequest.PriorityLevel))
                .ForPath(dest => dest.PurchaseRequest.Destination, opt => opt.MapFrom(src => src.PurchaseRequest.Destination))
                .ForPath(dest => dest.PurchaseRequest.RequestType, opt => opt.MapFrom(src => src.PurchaseRequest.RequestType))
                .ForPath(dest => dest.PurchaseRequest.RequestStatus, opt => opt.MapFrom(src => src.PurchaseRequest.RequestStatus))
                .ForPath(dest => dest.PurchaseRequest.IsAccountingApproved, opt => opt.MapFrom(src =>
                    src.PurchaseRequest.AccountingReview != null
                    && src.PurchaseRequest.AccountingReview.Status == AccountingReviewStatus.Approved))
                .ForPath(dest => dest.PurchaseRequest.IsManagementApproved, opt => opt.MapFrom(src =>
                    src.Status == ManagementReviewStatus.Approved))
                .ForPath(dest => dest.PurchaseRequest.IsPurchaseOrderGenerated, opt => opt.MapFrom(src =>
                    src.PurchaseRequest.PurchaseOrders.Any()));

            CreateMap<PurchaseRequestsReviewedManagement, PurchaseRequestsReviewedManagementDetailsDto>()
                .ForMember(dest => dest.PurchaseRequestsReviewedManagementId, opt => opt.MapFrom(src => src.Id))
                .ForPath(dest => dest.SentByUserInformation, opt => opt.MapFrom(src => src.SentByUser))
                .ForPath(dest => dest.ReviewerUserInformation, opt => opt.MapFrom(src => src.ReviewedByUser))
                .ForPath(dest => dest.PurchaseRequest, opt => opt.MapFrom(src => src.PurchaseRequest))
                .ForPath(dest => dest.PurchaseRequest.CostCenterInformation, opt => opt.MapFrom(src => src.PurchaseRequest.CostCenter))
                .ForPath(dest => dest.PurchaseRequest.BranchInformation, opt => opt.MapFrom(src => src.PurchaseRequest.Branch))
                .ForPath(dest => dest.PurchaseRequest.InformationFromRequestingArea, opt => opt.MapFrom(src => src.PurchaseRequest.WorkArea))
                .ForPath(dest => dest.PurchaseRequest.CreatorUserInformation, opt => opt.MapFrom(src => src.PurchaseRequest.RegistrationUser))
                .ForPath(dest => dest.PurchaseRequest.IsManagementApproved, opt => opt.MapFrom(src => src.Status == ManagementReviewStatus.Approved));
        }
    }

    public static class PurchaseRequestsReviewedManagementMapper
    {
        public static PurchaseRequestsReviewedManagement ToPurchaseRequestsReviewedManagementEntity(this Commands.SendPurchaseRequestToManagementReviewCommand request, Guid sentByUserId, Guid purchaseRequestId)
        {
            return new PurchaseRequestsReviewedManagement()
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
