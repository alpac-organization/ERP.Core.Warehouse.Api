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
            //Listado.
            CreateMap<PurchaseRequestsReviewedManagement, PurchaseRequestsReviewedManagementDto>()
                .ForMember(dest => dest.PurchaseRequestsReviewedManagementId, opt => opt.MapFrom(src => src.Id))

                .ForPath(dest => dest.SentByUserInformation, opt => opt.MapFrom(src => src.SentByUser))
                .ForPath(dest => dest.SentByUserInformation.WorkAreaInformation, opt => opt.MapFrom(src => src.SentByUser.WorkArea))

                .ForPath(dest => dest.PurchaseRequest, opt => opt.MapFrom(src => src.PurchaseRequest));

            //Detalle y padre.
            CreateMap<PurchaseRequestsReviewedManagement, PurchaseRequestsReviewedManagementDetailsDto>()
                .ForMember(dest => dest.PurchaseRequestsReviewedManagementId, opt => opt.MapFrom(src => src.Id))

                .ForPath(dest => dest.SentByUserInformation, opt => opt.MapFrom(src => src.SentByUser))
                .ForPath(dest => dest.SentByUserInformation.WorkAreaInformation, opt => opt.MapFrom(src => src.SentByUser.WorkArea))

                //Crear un metodo de validación y mapeo manual
                .ForPath(dest => dest.ReviewerUserInformation, opt => opt.MapFrom(src => src.ReviewedByUser))
                //your methond here

                .ForPath(dest => dest.PurchaseRequestDetails, opt => opt.MapFrom(src => src.PurchaseRequest))
                .ForPath(dest => dest.PurchaseRequestDetails.BranchInformation, opt => opt.MapFrom(src => src.PurchaseRequest.Branch))

                .ForPath(dest => dest.PurchaseRequestDetails.CreatorUserInformation, opt => opt.MapFrom(src => src.PurchaseRequest.RegistrationUser))
                .ForPath(dest => dest.PurchaseRequestDetails.CreatorUserInformation.WorkAreaInformation, opt => opt.MapFrom(src => src.PurchaseRequest.RegistrationUser.WorkArea));
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
