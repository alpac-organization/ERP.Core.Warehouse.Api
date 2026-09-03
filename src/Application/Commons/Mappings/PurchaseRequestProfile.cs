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
                .ForMember(dest => dest.PurchaseRequestId,     opt => opt.MapFrom(src => src.Id));
                
            CreateMap<PurchaseRequest, PurchaseRequestDetailsDto>()
                .ForMember(dest => dest.PurchaseRequestId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Observations, opt => opt.MapFrom(src => src.Concept))

                .ForPath(dest => dest.BranchInformation,      opt => opt.MapFrom(src => src.Branch))
                .ForPath(dest => dest.InformationFromRequestingArea, opt => opt.MapFrom(src => src.WorkArea))

                .ForPath(dest => dest.ReviewerUserInformation, opt => opt.MapFrom(src => src.UserRevision))
                .ForPath(dest => dest.CreatorUserInformation, opt => opt.MapFrom(src => src.RegistrationUser));
        }
    }

    public static class PurchaseRequestMapper
    {
        public static PurchaseRequest ToPurchaseRequestEntity(this Commands.RegisterPurchaseRequest command, string codeGenerated, Guid areaId, Guid userId)
        {
            return new()
            {
                AreaId              = areaId,
                Code                = codeGenerated,
                BranchId            = command.BranchId,

                UserRevisionId      = null,
                RegisteredByUserId  = userId,
                
                RequestType         = command.RequestType,
                Destination         = command.Destination,
                PriorityLevel       = command.PriorityLevel ?? PriorityLevel.None,
                
                Concept             = command.Observations,
                RequestStatus       = PurchaseRequestStatus.Pending,
                Id                  = Guid.NewGuid(),
                
                IsActive            = true,
                RequestDate         = DateOnly.FromDateTime(DateTime.UtcNow),
                RevisionDate        = null
            };
        }

        public static PurchaseRequestItem ToPurchaseRequestItemEntity(this Commands.PurchaseRequestItem command, Guid purchaseRequestId)
        {
            return new()
            {
                HasQuotation      = false,
                Id                = Guid.NewGuid(),
                PurchaseRequestId = purchaseRequestId,  
                Quantity          = command.Quantity,
                QuantityUnit      = command.QuantityUnit,
                ProductId         = command.ProductId,
                UnitMeasureId     = command.UnitMeasureId,
                Justification     = command.Justification,
                Description       = command.Description,
                AdditionalData    = command.AdditionalData
            };
        }
    }
}