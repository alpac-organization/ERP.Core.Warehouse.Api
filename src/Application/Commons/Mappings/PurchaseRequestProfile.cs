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
        }
    }

    public static class PurchaseRequestMapper
    {
        public static PurchaseRequest ToPurchaseRequestEntity(this Commands.RegisterPurchaseRequestCommand command, string codeGenerated)
        {
            return new()
            {
                Code          = codeGenerated,
                UserId        = command.UserId,
                BranchId      = command.BranchId,
                RequestType   = command.RequestType,
                Justification = command.Justification,
                RequestStatus = PurchaseRequestStatus.Pending,
                Id            = Guid.NewGuid(),
                RequestDate   = DateOnly.FromDateTime(DateTime.UtcNow),
            };
        }

        public static RequestedProduct ToRequestedProductEntity(this Commands.RequestedProduct command, Guid purchaseRequestId)
        {
            return new()
            {
                Id                = Guid.NewGuid(),
                Quantity          = command.Quantity,
                QuantityUnit      = command.QuantityUnit,
                ProductId         = command.ProductId,
                UnitMeasureId     = command.UnitMeasureId,
                PurchaseRequestId = purchaseRequestId,  
                Justification     = command.Justification
            };
        }
    }
}