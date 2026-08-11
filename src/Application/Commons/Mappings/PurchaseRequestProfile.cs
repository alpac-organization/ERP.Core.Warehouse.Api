using AutoMapper;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Shopping;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos;

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

                .ForPath(dest => dest.CreatorUserInformation.PictureUrl,     opt => opt.Ignore())
                .ForPath(dest => dest.CreatorUserInformation.UserId,     opt => opt.MapFrom(src => src.RegistrationUser.Id))
                .ForPath(dest => dest.CreatorUserInformation.Fullname,     opt => opt.MapFrom(src => src.RegistrationUser.Fullname))
                .ForPath(dest => dest.CreatorUserInformation.Email,     opt => opt.MapFrom(src => src.RegistrationUser.Email))

                .ForPath(dest => dest.ReviewerUserInformation.PictureUrl,     opt => opt.Ignore())
                .ForPath(dest => dest.ReviewerUserInformation.UserId,     opt => opt.MapFrom(src => src.RegistrationUser.Id))
                .ForPath(dest => dest.ReviewerUserInformation.Fullname,     opt => opt.MapFrom(src => src.RegistrationUser.Fullname))
                .ForPath(dest => dest.ReviewerUserInformation.Email,     opt => opt.MapFrom(src => src.RegistrationUser.Email))


                .ForPath(dest => dest.BranchInformation.BranchId,   opt => opt.MapFrom(src => src.Branch.Id))
                .ForPath(dest => dest.BranchInformation.BranchCode,   opt => opt.MapFrom(src => src.Branch.BranchCode))
                .ForPath(dest => dest.BranchInformation.BranchName,   opt => opt.MapFrom(src => src.Branch.BranchName))
                .ForPath(dest => dest.BranchInformation.CompanyAlias, opt => opt.MapFrom(src => src.Branch.CompanyAlias))

                .ForPath(dest => dest.RequestedProducts, opt => opt.Ignore());

            CreateMap<PurchaseRequestItem, ProductInformation>()
                .ForMember(dest => dest.PurchaseRequestId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                
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
}