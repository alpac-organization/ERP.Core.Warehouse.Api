using AutoMapper;
using ERP.Core.Database.Domain.Entities.Shopping;
using ERP.Core.Warehouse.Api.Application.Features.RequisitionAccountingReviews.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{
    public class RequisitionAccountingReviewsProfile : Profile
    {
        public RequisitionAccountingReviewsProfile()
        {
            CreateMap<RequisitionAccountingReview, RequisitionAccountingReviewDto>()
                .ForMember(dest => dest.RequisitionAccountingReviewId, opt => opt.MapFrom(src => src.Id))

                .ForPath(dest => dest.ReviewerUserInformation.PictureUrl, opt => opt.Ignore())
                .ForPath(dest => dest.ReviewerUserInformation.UserId, opt => opt.MapFrom(src => src.ReviewedByUser!.Id))
                .ForPath(dest => dest.ReviewerUserInformation.Fullname, opt => opt.MapFrom(src => src.ReviewedByUser!.Fullname))
                .ForPath(dest => dest.ReviewerUserInformation.Email, opt => opt.MapFrom(src => src.ReviewedByUser!.Email))

                .ForMember(dest => dest.PurchaseRequest, opt => opt.MapFrom(src => src.PurchaseRequest));

            CreateMap<RequisitionAccountingReview, RequisitionAccountingReviewDetailsDto>()
                .ForMember(dest => dest.RequisitionAccountingReviewId, opt => opt.MapFrom(src => src.Id))

                .ForPath(dest => dest.ReviewerUserInformation.PictureUrl, opt => opt.Ignore())
                .ForPath(dest => dest.ReviewerUserInformation.UserId, opt => opt.MapFrom(src => src.ReviewedByUser!.Id))
                .ForPath(dest => dest.ReviewerUserInformation.Fullname, opt => opt.MapFrom(src => src.ReviewedByUser!.Fullname))
                .ForPath(dest => dest.ReviewerUserInformation.Email, opt => opt.MapFrom(src => src.ReviewedByUser!.Email))

                .ForMember(dest => dest.PurchaseRequest, opt => opt.MapFrom(src => src.PurchaseRequest));

            CreateMap<PurchaseRequest, PurchaseRequestRawInformationDto>()
                .ForMember(dest => dest.PurchaseRequestId, opt => opt.MapFrom(src => src.Id))

                .ForPath(dest => dest.BranchInformation.BranchId, opt => opt.MapFrom(src => src.Branch.Id))
                .ForPath(dest => dest.BranchInformation.BranchCode, opt => opt.MapFrom(src => src.Branch.BranchCode))
                .ForPath(dest => dest.BranchInformation.BranchName, opt => opt.MapFrom(src => src.Branch.BranchName))
                .ForPath(dest => dest.BranchInformation.CompanyAlias, opt => opt.MapFrom(src => src.Branch.CompanyAlias))

                .ForPath(dest => dest.WorkAreaInformation.WorkAreaId, opt => opt.MapFrom(src => src.WorkArea.Id))
                .ForPath(dest => dest.WorkAreaInformation.WorkAreaCode, opt => opt.MapFrom(src => src.WorkArea.WorkAreaCode))
                .ForPath(dest => dest.WorkAreaInformation.Description, opt => opt.MapFrom(src => src.WorkArea.Description))
                .ForPath(dest => dest.WorkAreaInformation.WorkAreaName, opt => opt.MapFrom(src => src.WorkArea.WorkAreaName))

                .ForPath(dest => dest.CreatorUserInformation.PictureUrl, opt => opt.Ignore())
                .ForPath(dest => dest.CreatorUserInformation.UserId, opt => opt.MapFrom(src => src.RegistrationUser.Id))
                .ForPath(dest => dest.CreatorUserInformation.Fullname, opt => opt.MapFrom(src => src.RegistrationUser.Fullname))
                .ForPath(dest => dest.CreatorUserInformation.Email, opt => opt.MapFrom(src => src.RegistrationUser.Email))

                .ForPath(dest => dest.ReviewerUserInformation.PictureUrl, opt => opt.Ignore())
                .ForPath(dest => dest.ReviewerUserInformation.UserId, opt => opt.MapFrom(src => src.UserRevision.Id))
                .ForPath(dest => dest.ReviewerUserInformation.Fullname, opt => opt.MapFrom(src => src.UserRevision.Fullname))
                .ForPath(dest => dest.ReviewerUserInformation.Email, opt => opt.MapFrom(src => src.UserRevision.Email))

                .ForMember(dest => dest.RequestedProducts, opt => opt.Ignore());

            CreateMap<PurchaseRequestItem, PurchaseRequestItemRawInformationDto>()
                .ForMember(dest => dest.PurchaseRequestItemId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.PurchaseRequestId, opt => opt.MapFrom(src => src.PurchaseRequestId))

                .ForPath(dest => dest.ProductDetails.ProductId, opt => opt.MapFrom(src => src.Product.Id))
                .ForPath(dest => dest.ProductDetails.ProductName, opt => opt.MapFrom(src => src.Product.ProductName))

                .ForPath(dest => dest.ProductDetails.CategoryInformation.CatagoryId, opt => opt.MapFrom(src => src.Product.Category.Id))
                .ForPath(dest => dest.ProductDetails.CategoryInformation.Code, opt => opt.MapFrom(src => src.Product.Category.Code))
                .ForPath(dest => dest.ProductDetails.CategoryInformation.Name, opt => opt.MapFrom(src => src.Product.Category.Name))

                .ForPath(dest => dest.UnitMeasureInformation.Code, opt => opt.MapFrom(src => src.UnitMeasure.Code))
                .ForPath(dest => dest.UnitMeasureInformation.Name, opt => opt.MapFrom(src => src.UnitMeasure.Name))
                .ForPath(dest => dest.UnitMeasureInformation.Symbol, opt => opt.MapFrom(src => src.UnitMeasure.Symbol));

            CreateMap<Quotation, QuotationInformationDto>()
                .ForMember(dest => dest.QuotationId, opt => opt.MapFrom(src => src.Id))

                .ForPath(dest => dest.SupplierInformation.SupplierId, opt => opt.MapFrom(src => src.Supplier.Id))
                .ForPath(dest => dest.SupplierInformation.ImageUrl, opt => opt.MapFrom(src => src.Supplier.ImageUrl))
                .ForPath(dest => dest.SupplierInformation.SuppliersLegalName, opt => opt.MapFrom(src => src.Supplier.SuppliersLegalName))
                .ForPath(dest => dest.SupplierInformation.IdentificationNumber, opt => opt.MapFrom(src => src.Supplier.IdentificationNumber))
                .ForPath(dest => dest.SupplierInformation.IdentificationType, opt => opt.MapFrom(src => src.Supplier.IdentificationType));
        }
    }
}
