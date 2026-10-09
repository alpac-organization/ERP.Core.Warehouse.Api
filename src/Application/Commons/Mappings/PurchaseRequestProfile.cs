using AutoMapper;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Shopping;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos;
using Commands = ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{

    public class PurchaseRequestProfile : Profile
    {
        public PurchaseRequestProfile()
        {
            CreateMap<PurchaseRequest, PurchaseRequestDto>()
                .ForMember(dest => dest.PurchaseRequestId,opt => opt.MapFrom(src => src.Id))
                .ForPath(dest => dest.AnnulledByUserInformation, opt => opt.MapFrom(src => src.AnnulledByUser));
                
            CreateMap<PurchaseRequest, PurchaseRequestDetailsDto>()
                .ForMember(dest => dest.PurchaseRequestId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Observations, opt => opt.MapFrom(src => src.Concept))
                .ForMember(dest => dest.ReasonRejection, opt => opt.MapFrom(src => ExtractReasonRejection(src.AdditionalData)))
                .ForMember(dest => dest.IsManagementApproved, opt => opt.MapFrom(src => src.ManagementReview != null && src.ManagementReview.Status == ManagementReviewStatus.Approved))
                .ForMember(dest => dest.IsAccountingApproved, opt => opt.MapFrom(src => src.AccountingReview != null && src.AccountingReview.Status == AccountingReviewStatus.Approved))
                .ForMember(dest => dest.IsPurchaseOrderGenerated, opt => opt.MapFrom(src => src.PurchaseOrders != null && src.PurchaseOrders.Any()))
                .ForMember(dest => dest.PurchaseRequestItems, opt => opt.MapFrom(src => src.PurchaseRequestItems))
                
                .ForPath(dest => dest.BranchInformation,      opt => opt.MapFrom(src => src.Branch))
                .ForPath(dest => dest.CostCenterInformation, opt => opt.MapFrom(src => src.CostCenter))
                .ForPath(dest => dest.InformationFromRequestingArea, opt => opt.MapFrom(src => src.WorkArea))

                .ForPath(dest => dest.ReviewerUserInformation, opt => opt.MapFrom(src => src.UserRevision))
                .ForPath(dest => dest.CreatorUserInformation, opt => opt.MapFrom(src => src.RegistrationUser))
                .ForPath(dest => dest.AnnulledByUserInformation, opt=>opt.MapFrom(src=> src.AnnulledByUser));
        }

        private static string? ExtractReasonRejection(string? additionalDataJson)
        {
            if (string.IsNullOrWhiteSpace(additionalDataJson)) return null;
            try
            {
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower, PropertyNameCaseInsensitive = true };
                var history = System.Text.Json.JsonSerializer.Deserialize<List<ERP.Core.Database.Domain.Entities.Shopping.PurchaseRequestAdditionalData>>(additionalDataJson, options);
                var rejectionEntry = history?.LastOrDefault(h => h.Description == "Rechazo de solicitud");
                return rejectionEntry?.NewField;
            }
            catch
            {
                return null;
            }
        }
    }

    public static class PurchaseRequestMapper
    {
        public static PurchaseRequest ToPurchaseRequestEntity(
            this Commands.RegisterPurchaseRequest command,
            string codeGenerated,
            Guid areaId,
            Guid userId,
            DateOnly requestDate)
        {
            return new()
            {
                AreaId              = areaId,
                Code                = codeGenerated,
                BranchId            = command.BranchId,
                CostCenterId        = command.CostCenterId,
                UserRevisionId      = null,
                RegisteredByUserId  = userId,
                
                RequestType         = command.RequestType,
                Destination         = command.Destination,
                PriorityLevel       = command.PriorityLevel ?? PriorityLevel.None,
                
                Concept             = command.Observations,
                RequestStatus       = PurchaseRequestStatus.Pending,
                Id                  = Guid.NewGuid(),
                
                IsActive            = true,
                RequestDate         = requestDate,
                RevisionDate        = null
            };
        }

        public static PurchaseRequestItem ToPurchaseRequestItemEntity(this Commands.PurchaseRequestItem command, Guid purchaseRequestId, Guid productId, Guid unitMeasureId)
        {
            return new()
            {
                HasQuotation      = false,
                Id                = Guid.NewGuid(),
                PurchaseRequestId = purchaseRequestId,
                Quantity          = command.Quantity,
                QuantityUnit      = command.QuantityUnit,
                ProductId         = productId,
                UnitMeasureId     = unitMeasureId,
                Justification     = command.Justification,
                Description       = command.Description,
                AdditionalData    = command.AdditionalData
            };
        }

        public static PurchaseRequestItem ToPurchaseRequestItemEntity(this Commands.UpdatePurchaseRequestItem command, Guid purchaseRequestId)
        {
            return new()
            {
                HasQuotation      = false,
                Id                = Guid.NewGuid(),
                PurchaseRequestId = purchaseRequestId,
                Quantity          = command.Quantity!.Value,
                QuantityUnit      = command.QuantityUnit,
                ProductId         = command.ProductId!.Value,
                UnitMeasureId     = command.UnitMeasureId!.Value,
                Justification     = command.Justification,
                Description       = command.Description,
                AdditionalData    = null
            };
        }
    }
}