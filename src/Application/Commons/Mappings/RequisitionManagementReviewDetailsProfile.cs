using AutoMapper;
using ERP.Core.Database.Domain.Entities.Shopping;
using ERP.Core.Warehouse.Api.Application.Features.RequisitionManagementReviews.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{
    public class RequisitionManagementReviewDetailsProfile : Profile
    {
        public RequisitionManagementReviewDetailsProfile()
        {
             CreateMap<RequisitionManagementReview, RequisitionManagementReviewDetailsDto>()
                .ForMember(dest => dest.RequisitionManagementReviewId, opt => opt.MapFrom(src => src.Id))
                
                .ForPath(dest => dest.SentByUserInformation, opt => opt.MapFrom(src => src.SentByUser))
                // .ForPath(dest => dest.SentByUserInformation.WorkAreaInformation, opt => opt.MapFrom(src => src.SentByUser.WorkArea))

                .ForPath(dest => dest.ReviewerUserInformation, opt => opt.MapFrom(src => src.ReviewedByUser))

                .ForPath(dest => dest.PurchaseRequestDetails, opt => opt.MapFrom(src => src.PurchaseRequest))
                .ForPath(dest => dest.PurchaseRequestDetails.BranchInformation, opt => opt.MapFrom(src => src.PurchaseRequest.Branch))

                .ForPath(dest => dest.PurchaseRequestDetails.CreatorUserInformation, opt => opt.MapFrom(src => src.PurchaseRequest.RegistrationUser))
                .ForPath(dest => dest.PurchaseRequestDetails.CreatorUserInformation.WorkAreaInformation, opt => opt.MapFrom(src => src.PurchaseRequest.RegistrationUser.WorkArea))
                
                ;
        }
    }
}