using AutoMapper;
using ERP.Core.Database.Domain.Entities.Bases;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Shopping;

using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos;
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
                .ForMember(dest => dest.SentByUserInformation, opt => opt.MapFrom(src => new UserInformation
                {
                    UserId = src.SentByUser.Id,
                    Email = src.SentByUser.Email,
                    Fullname = src.SentByUser.Fullname,
                    PictureUrl = src.SentByUser.PictureUrl,
                    UserStatus = src.SentByUser.UserStatus,
                    WorkAreaInformation = src.SentByUser.Profiles
                        .Where(profile => profile.CompanyId == profileCompanyId && profile.WorkArea != null)
                        .Select(profile => new WorkAreaInformation
                        {
                            WorkAreaId = profile.WorkArea.Id,
                            Description = profile.WorkArea.Description,
                            WorkAreaName = profile.WorkArea.WorkAreaName
                        })
                        .FirstOrDefault()
                }))
                .ForMember(dest => dest.PurchaseRequest, opt => opt.MapFrom(src => new PurchaseRequestSummaryDto
                {
                    PurchaseRequestId = src.PurchaseRequest.Id,
                    Code = src.PurchaseRequest.Code,
                    RequestDate = src.PurchaseRequest.RequestDate,
                    RevisionDate = src.PurchaseRequest.RevisionDate,
                    PriorityLevel = src.PurchaseRequest.PriorityLevel,
                    Destination = src.PurchaseRequest.Destination,
                    RequestType = src.PurchaseRequest.RequestType,
                    RequestStatus = src.PurchaseRequest.RequestStatus,
                    IsAccountingApproved = src.PurchaseRequest.AccountingReview != null
                        && src.PurchaseRequest.AccountingReview.Status == AccountingReviewStatus.Approved,
                    IsManagementApproved = src.Status == ManagementReviewStatus.Approved,
                    IsPurchaseOrderGenerated = src.PurchaseRequest.PurchaseOrders.Any()
                }));

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
