using AutoMapper;
using ERP.Core.Database.Domain.Entities.Auth;
using ERP.Core.Database.Domain.Entities.Bases;
using ERP.Core.Database.Domain.Entities.Bases.Payroll;

using Profiles = ERP.Core.Database.Domain.Entities.Auth.UserProfile;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{
    public class UserProfile : Profile
    {
        public UserProfile()
        {
            CreateMap<User, UserInformation>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id))
                .ForPath(dest => dest.WorkAreaInformation, opt => opt.MapFrom(src => src.Profiles.FirstOrDefault()));

            CreateMap<Profiles, WorkAreaInformation>()
                .ForMember(dest => dest.WorkAreaId, opt => opt.MapFrom(src => src.WorkArea.Id))
                .ForMember(dest => dest.WorkAreaCode, opt => opt.MapFrom(src => src.WorkArea.WorkAreaCode))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.WorkArea.Description))
                .ForMember(dest => dest.WorkAreaName, opt => opt.MapFrom(src => src.WorkArea.WorkAreaName));

            CreateMap<Profiles, WorkingInformation>()
                .ForPath(dest => dest.WorkAreaInformation, opt => opt.MapFrom(src => src.WorkArea));
        }
    }
}