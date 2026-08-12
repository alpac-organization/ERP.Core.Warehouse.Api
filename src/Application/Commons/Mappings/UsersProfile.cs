using AutoMapper;
using ERP.Core.Database.Domain.Entities.Auth;
using ERP.Core.Warehouse.Api.Application.Features.RequisitionAccountingReviews.v1.Dtos;

using PurchaseRequest = ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{
    public class UserProfile : Profile
    {
        public UserProfile()
        {
            CreateMap<User, SentByUserInformation>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id));
            
            CreateMap<User, PurchaseRequest.CreatorUserInformation>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id));

            CreateMap<User, PurchaseRequest.ReviewerUserInformation>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id));
        }
    }
}