using AutoMapper;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Inventory;
using ERP.Core.Warehouse.Api.Application.Features.FuelRequests.v1.Dtos;

using DbUserProfile = ERP.Core.Database.Domain.Entities.Auth.UserProfile;
using Commands = ERP.Core.Warehouse.Api.Application.Features.FuelRequests.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings;

public class FuelRequestsProfile : Profile
{
    public FuelRequestsProfile()
    {
        CreateMap<FuelRequest, GetFuelRequestDto>()
            .ForMember(dest => dest.FuelRequestId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.FuelRequestCode, opt => opt.MapFrom(src => src.Code))
            .ForMember(dest => dest.MachineryCode, opt => opt.MapFrom(src => src.Machinery.Code))
            .ForMember(dest => dest.RequestedByUserName, opt => opt.MapFrom(src => src.User.Fullname));

        CreateMap<FuelRequest, GetFuelRequestDetailDto>()
            .ForMember(dest => dest.FuelRequestId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.FuelRequestCode, opt => opt.MapFrom(src => src.Code))
            .ForMember(dest => dest.MachineryCode, opt => opt.MapFrom(src => src.Machinery.Code))
            .ForMember(dest => dest.RequestedByUserName, opt => opt.MapFrom(src => src.User.Fullname))
            .ForMember(dest => dest.WorkAreaInformation, opt => opt.MapFrom(src => src.WorkArea))
            .ForMember(dest => dest.CostCenterInformation, opt => opt.MapFrom(src => src.CostCenter))
            .ForMember(dest => dest.MachineryInformation, opt => opt.MapFrom(src => src.Machinery))
            .ForMember(dest => dest.ProductStockName, opt => opt.MapFrom(src => src.ProductStock.Product.ProductName))
            .ForMember(dest => dest.BranchInformation, opt => opt.MapFrom(src => src.Branch))
            .ForMember(dest => dest.ApprovedByUserChiefName, opt => opt.Ignore())
            .ForMember(dest => dest.AnnulledByUserChiefName, opt => opt.Ignore());
    }
}

public static class FuelRequestMapper
{
    public static FuelRequest ToFuelRequestEntity(
        this Commands.RegisterFuelRequestsCommand request,
        DbUserProfile userProfile,
        string code,
        Guid unitMeasureId)
    {
        return new()
        {
            Id = Guid.NewGuid(),
            Code = code,
            Justification = request.Justification,
            OutflowStatus = OutflowStatus.PendingReview,
            WorkAreaId = userProfile.AreaId,
            CostCenterId = userProfile.CostCenterId,
            BranchId = userProfile.BranchId,
            MachineryId = request.MachineryId,
            ProductStockId = request.ProductStockId,
            UnitMeasureId = unitMeasureId,
            RequestedQuantity = request.RequestedQuantity,
            RequestedByUserId = userProfile.UserId,
            Mileage = request.Mileage,
            EngineHours = request.EngineHours
        };
    }
}