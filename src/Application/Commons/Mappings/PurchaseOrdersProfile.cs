using AutoMapper;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Bases;
using ERP.Core.Database.Domain.Entities.Shopping;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{
    public class PurchaseOrdersProfile : Profile
    {
        public PurchaseOrdersProfile()
        {
            Guid profileCompanyId = default;

            CreateMap<PurchaseOrder, PurchaseOrderDto>()
                .ForMember(dest => dest.PurchaseOrderId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.PurchaseOrderCode, opt => opt.MapFrom(src => src.Code))
                .ForMember(dest => dest.PaymentRequestCode, opt => opt.MapFrom(src => src.PaymentRequest != null ? src.PaymentRequest.Code : null))
                .ForMember(dest => dest.SupplierName, opt => opt.MapFrom(src => src.Supplier != null ? src.Supplier.SuppliersLegalName : null))
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
                    IsManagementApproved = src.PurchaseRequest.ManagementReview != null
                        && src.PurchaseRequest.ManagementReview.Status == ManagementReviewStatus.Approved,
                    IsPurchaseOrderGenerated = src.PurchaseRequest.PurchaseOrders.Any()
                }));

            CreateMap<PurchaseOrder, PurchaseOrderDetailsDto>()
                .ForMember(dest => dest.PurchaseOrderId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.PurchaseOrderCode, opt => opt.MapFrom(src => src.Code))
                .ForMember(dest => dest.SupplierId, opt => opt.MapFrom(src => src.SupplierId))
                .ForMember(dest => dest.SupplierName, opt => opt.MapFrom(src => src.Supplier != null ? src.Supplier.SuppliersLegalName : null))
                .ForPath(dest => dest.SentByUserInformation, opt => opt.MapFrom(src => src.SentByUser))
                .ForPath(dest => dest.ReviewerUserInformation, opt => opt.MapFrom(src => src.ReviewedByUser));

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

    }
}   
