using AutoMapper;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Inventory;

using ERP.Core.Warehouse.Api.Application.Features.SupplyRequests.v1.Dtos;

using Commands = ERP.Core.Warehouse.Api.Application.Features.SupplyRequests.v1.Commands;
using DbUserProfile = ERP.Core.Database.Domain.Entities.Auth.UserProfile;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Database.Domain.Entities.Operations;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings;

public class SupplyRequestsProfile : Profile
{
    public SupplyRequestsProfile()
    {
        CreateMap<SupplyRequest, GetSupplyRequestDto>()
            .ForMember(dest => dest.SupplyRequestId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.SupplyRequestCode, opt => opt.MapFrom(src => src.Code))
            .ForMember(dest => dest.RequestedByUserName, opt => opt.MapFrom(src => src.User.Fullname))
            .ForMember(dest => dest.ItemsCount, opt => opt.MapFrom(src => src.SupplyRequestItems.Count));

        CreateMap<SupplyRequest, GetSupplyRequestDetailDto>()
            .ForMember(dest => dest.SupplyRequestId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.SupplyRequestCode, opt => opt.MapFrom(src => src.Code))
            .ForMember(dest => dest.WorkAreaInformation, opt => opt.MapFrom(src => src.WorkArea))
            .ForMember(dest => dest.RequestedByUserName, opt => opt.MapFrom(src => src.User.Fullname))
            .ForMember(dest => dest.CostCenterInformation, opt => opt.MapFrom(src => src.CostCenter))
            .ForMember(dest => dest.BranchInformation, opt => opt.MapFrom(src => src.Branch))
            .ForMember(dest => dest.WarehouseInformation, opt => opt.MapFrom(src => src.Warehouse))
            .ForMember(dest => dest.OperationalOrderInformation, opt => opt.MapFrom(src => src.OperationalOrder))
            .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.SupplyRequestItems));

        CreateMap<WorkArea, WorkAreaInformation>()
            .ForMember(dest => dest.WorkAreaId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.WorkAreaName, opt => opt.MapFrom(src => src.WorkAreaName));

        CreateMap<CostCenter, CostCenterInformation>()
            .ForMember(dest => dest.CostCenterId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.CostCenterName, opt => opt.MapFrom(src => src.CostCenterName));

        CreateMap<Branch, BranchInformation>()
            .ForMember(dest => dest.BranchId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.BranchCode, opt => opt.MapFrom(src => src.BranchCode));

        CreateMap<Warehouses, WarehouseInformation>()
            .ForMember(dest => dest.WarehouseId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.WarehouseCode, opt => opt.MapFrom(src => src.Code));

        CreateMap<Warehouses, WarehouseInformation>()
            .ForMember(dest => dest.WarehouseId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.WarehouseCode, opt => opt.MapFrom(src => src.Code));

        CreateMap<OperationalOrder, OperationalOrderInformation>()
            .ForMember(dest => dest.OperationalOrderId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.OperationalOrderCode, opt => opt.MapFrom(src => src.PoCode));

        CreateMap<SupplyRequestItem, ItemDetail>()
            .ForMember(dest => dest.SupplyRequestItemId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.ProductStockName, opt => opt.MapFrom(src => src.ProductStock.Product.ProductName));
    }
}

public static class SupplyRequestMapper
{
    public static SupplyRequest ToSupplyRequestEntity(
        this Commands.RegisterSupplyRequestsCommad request, DbUserProfile userProfile, string code,
        Guid warehouseId
    )
    {
        var id = Guid.NewGuid();
        return new()
        {
            Id = id,
            Code = code,
            Justification = request.Justification,
            OutflowStatus = OutflowStatus.PendingReview,
            WorkAreaId = userProfile.AreaId,
            CostCenterId = userProfile.CostCenterId,
            BranchId = userProfile.BranchId,
            WarehouseId = warehouseId,
            OperationalOrderId = request.OperationalOrderId,
            RequestedByUserId = userProfile.UserId,
            SupplyRequestItems = [.. request.Items.Select(item => item.ToSupplyRequestItemEntity(id))]
        };
    }

    public static SupplyRequestItem ToSupplyRequestItemEntity(
        this Commands.RegisterSupplyRequestItem command, Guid supplyRequestId)
    {
        return new()
        {
            Id = Guid.NewGuid(),
            IsActive = true,
            ProductStockId = command.ProductStockId,
            Amount = command.Amount,
            ReasonUse = ReasonUse.Operation,
            SupplyRequestId = supplyRequestId
        };
    }
}