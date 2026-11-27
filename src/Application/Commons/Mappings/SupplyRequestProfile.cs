using AutoMapper;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Inventory;

using ERP.Core.Warehouse.Api.Application.Features.SupplyRequests.v1.Dtos;

using DbUserProfile = ERP.Core.Database.Domain.Entities.Auth.UserProfile;
using Commands = ERP.Core.Warehouse.Api.Application.Features.SupplyRequests.v1.Commands;
namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings;

public class SupplyRequestsProfile : Profile
{
    public SupplyRequestsProfile()
    {
        CreateMap<SupplyRequest, GetSupplyRequestDto>()
            .ForMember(dest => dest.SupplyRequestId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.SupplyRequestCode, opt => opt.MapFrom(src => src.Code))
            .ForMember(dest => dest.PoCode, opt => opt.MapFrom(src => src.OperationalOrder != null ? src.OperationalOrder.PoCode : null))
            .ForMember(dest => dest.RequestedByUserName, opt => opt.MapFrom(src => src.User.Fullname))
            .ForMember(dest => dest.WorkAreaName, opt => opt.MapFrom(src => src.WorkArea.WorkAreaName))
            .ForMember(dest => dest.ItemsCount, opt => opt.MapFrom(src => src.SupplyRequestItems.Count))
            .ForMember(dest => dest.WarehouseCode, opt => opt.MapFrom(src => src.Warehouse != null ? src.Warehouse.Code : null))
            .ForMember(dest => dest.ReasonUse, opt => opt.MapFrom(src =>
                            src.SupplyRequestItems.Select(i => i.ReasonUse).FirstOrDefault()));

        CreateMap<SupplyRequest, GetSupplyRequestDetailDto>()
            .ForMember(dest => dest.SupplyRequestId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.SupplyRequestCode, opt => opt.MapFrom(src => src.Code))
            .ForMember(dest => dest.ReasonUse, opt => opt.MapFrom(src =>
                            src.SupplyRequestItems.Select(i => i.ReasonUse).FirstOrDefault()))
            .ForMember(dest => dest.WorkAreaInformation, opt => opt.MapFrom(src => src.WorkArea))
            .ForMember(dest => dest.RequestedByUserName, opt => opt.MapFrom(src => src.User.Fullname))
            .ForMember(dest => dest.CostCenterInformation, opt => opt.MapFrom(src => src.CostCenter))
            .ForMember(dest => dest.BranchInformation, opt => opt.MapFrom(src => src.Branch))
            .ForMember(dest => dest.WarehouseInformation, opt => opt.MapFrom(src => src.Warehouse))
            .ForMember(dest => dest.ApprovedByUserChiefName, opt => opt.Ignore())
            .ForMember(dest => dest.AnnulledByUserChiefName, opt => opt.Ignore())
            .ForMember(dest => dest.OperationalOrderInformation, opt => opt.MapFrom(src => src.OperationalOrder))
            .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.SupplyRequestItems))
            .ForMember(dest => dest.EquipmentInformation, opt => opt.MapFrom(src => src.Equipment))
            .ForMember(dest => dest.CustomerInformation, opt => opt.MapFrom(src => src.Customer));

        CreateMap<SupplyRequestItem, ItemDetail>()
            .ForMember(dest => dest.SupplyRequestItemId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.ProductStockName, opt => opt.MapFrom(src => src.ProductStock.Product.ProductName))
            .ForMember(dest => dest.UnitMeasureSymbol, opt => opt.MapFrom(src =>
                src.ProductStock != null && src.ProductStock.UnitMeasure != null
                    ? src.ProductStock.UnitMeasure.Symbol
                    : null));
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
            SupplyRequestItems = [.. request.Items.Select(item => item.ToSupplyRequestItemEntity(id, request.ReasonUse))]
        };
    }

    // ============================================================
    // FLUJO PARALELO TEMPORAL - SE ELIMINARÁ EN EL FUTURO
    // Mapeo del flujo manual: WarehouseId y campos manuales desde el request.
    // ============================================================
    public static SupplyRequest ToSupplyRequestManualEntity(
        this Commands.RegisterSupplyRequestManualCommand request, DbUserProfile userProfile, string code)
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
            WarehouseId = request.WarehouseId,
            RequestedByUserId = userProfile.UserId,
            EquipmentId = request.EquipmentId,
            CustomerId = request.CustomerId,
            DucaOrTransportOrder = request.DucaOrTransportOrder,
            SupplyRequestItems = [.. request.Items.Select(item => item.ToSupplyRequestItemEntity(id, request.ReasonUse))]
        };
    }

    public static SupplyRequestItem ToSupplyRequestItemEntity(
        this Commands.RegisterSupplyRequestItem command, Guid supplyRequestId, ReasonUse reasonUse)
    {
        return new()
        {
            Id = Guid.NewGuid(),
            IsActive = true,
            ProductStockId = command.ProductStockId,
            Amount = command.Amount,
            ReasonUse = reasonUse,
            SupplyRequestId = supplyRequestId
        };
    }
}