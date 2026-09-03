using AutoMapper;
using ERP.Core.Database.Domain.Entities.Auth;
using ERP.Core.Database.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.RequisitionAccountingReviews.v1.Dtos;

using PurchaseRequest = ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{
    public class UserProfile : Profile
    {
        public UserProfile()
        {
            CreateMap<User, UserInformation>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id))
                .ForPath(dest => dest.WorkAreaInformation, opt => opt.MapFrom(src => src.WorkArea));
        }
    }
}