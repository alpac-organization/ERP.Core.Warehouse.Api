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
                .ForMember(dest => dest.PurchaseRequestId,          opt => opt.MapFrom(src => src.Id))

                .ForPath(dest => dest.UserInformation.UserId,       opt => opt.MapFrom(src => src.User.Id))
                .ForPath(dest => dest.UserInformation.Email,        opt => opt.MapFrom(src => src.User.Email))
                .ForPath(dest => dest.UserInformation.Fullname,     opt => opt.MapFrom(src => src.User.Fullname))

                .ForPath(dest => dest.BranchInformation.BranchId,     opt => opt.MapFrom(src => src.Branch.Id))
                .ForPath(dest => dest.BranchInformation.BranchCode,   opt => opt.MapFrom(src => src.Branch.BranchCode))
                .ForPath(dest => dest.BranchInformation.BranchName,   opt => opt.MapFrom(src => src.Branch.BranchName))
                .ForPath(dest => dest.BranchInformation.CompanyAlias, opt => opt.MapFrom(src => src.Branch.CompanyAlias))

                .ForPath(dest => dest.RequestedProducts, opt => opt.Ignore());

            CreateMap<RequestedProduct, ProductInformation>()
                .ForMember(dest => dest.PurchaseRequestId, opt => opt.MapFrom(src => src.Id))
                
                .ForPath(dest => dest.ProductDetails.ProductId,   opt => opt.MapFrom(src => src.Product.Id))
                .ForPath(dest => dest.ProductDetails.ProductName, opt => opt.MapFrom(src => src.Product.ProductName))

                .ForPath(dest => dest.ProductDetails.CategoryInformation.CatagoryId, opt => opt.MapFrom(src => src.Product.Category.Id))
                .ForPath(dest => dest.ProductDetails.CategoryInformation.Code,       opt => opt.MapFrom(src => src.Product.Category.Code))                
                .ForPath(dest => dest.ProductDetails.CategoryInformation.Name,       opt => opt.MapFrom(src => src.Product.Category.Name))

                .ForPath(dest => dest.UnitMeasureInformation.Code,   opt => opt.MapFrom(src => src.UnitMeasure.Code))
                .ForPath(dest => dest.UnitMeasureInformation.Name,   opt => opt.MapFrom(src => src.UnitMeasure.Name))
                .ForPath(dest => dest.UnitMeasureInformation.Symbol, opt => opt.MapFrom(src => src.UnitMeasure.Symbol));
        }
    }

    public static class PurchaseRequestMapper
    {
        public static PurchaseRequest ToPurchaseRequestEntity(this Commands.RegisterPurchaseRequestCommand command, string codeGenerated, Guid areaId)
        {
            return new()
            {
                AreaId        = areaId,
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