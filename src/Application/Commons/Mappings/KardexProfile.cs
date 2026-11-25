using AutoMapper;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Inventory;
using ERP.Core.Warehouse.Api.Application.Features.InventoryStocks.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{
    public class KardexProfile : Profile
    {
        public KardexProfile()
        {
            CreateMap<Kardex, KardexDto>()
                .ForMember(dest => dest.KardexId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ProductInformation, opt => opt.MapFrom(src => src.Product))
                .ForMember(dest => dest.UnitMeasureInformation, opt => opt.MapFrom(src => src.Product.UnitMeasure))
                .ForMember(dest => dest.CostCenterInformation, opt => opt.MapFrom(src => src.CostCenter))
                .ForMember(dest => dest.WorkAreaInformation, opt => opt.MapFrom(src => src.WorkArea))
                .ForMember(dest => dest.BranchInformation, opt => opt.MapFrom(src => src.Product.Branch));

            CreateMap<Kardex, KardexDetailsDto>()
                .IncludeBase<Kardex, KardexDto>()
                .ForMember(dest => dest.MovementByUserInformation, opt => opt.MapFrom(src => src.User))
                .ForMember(dest => dest.PreviousUnits, opt => opt.MapFrom(src =>
                    src.MovementType == KardexMovementType.Exit
                        ? src.TotalUnits + src.Units
                        : (src.TotalUnits - src.Units < 0m ? 0m : src.TotalUnits - src.Units)))
                .ForMember(dest => dest.PreviousKardex, opt => opt.MapFrom(src =>
                    src.MovementType == KardexMovementType.Exit
                        ? src.TotalKardex + src.TotalOutput
                        : (src.TotalKardex - src.TotalInput < 0m ? 0m : src.TotalKardex - src.TotalInput)))
                .ForMember(dest => dest.InventoryOutputCode, opt => opt.MapFrom(src =>
                    src.InventoryOutput != null ? src.InventoryOutput.Code : null))
                .ForMember(dest => dest.InventoryOutputStatus, opt => opt.MapFrom(src =>
                    src.InventoryOutput != null ? (InventoryOutputsStatus?)src.InventoryOutput.Status : null))
                .ForMember(dest => dest.ReasonRemovalDescription, opt => opt.MapFrom(src =>
                    src.InventoryOutput != null && src.InventoryOutput.ReasonsInventoryRemoval != null
                        ? src.InventoryOutput.ReasonsInventoryRemoval.Description
                        : null))
                .ForMember(dest => dest.OutputRequestCode, opt => opt.MapFrom(src =>
                    src.InventoryOutput != null
                        ? (src.InventoryOutput.SupplyRequest != null
                            ? src.InventoryOutput.SupplyRequest.Code
                            : src.InventoryOutput.FuelRequest != null
                                ? src.InventoryOutput.FuelRequest.Code
                                : null)
                        : null))
                .ForMember(dest => dest.OutputRequestType, opt => opt.MapFrom(src =>
                    src.InventoryOutput != null
                        ? (src.InventoryOutput.SupplyRequestId.HasValue
                            ? "Supply"
                            : src.InventoryOutput.FuelRequestId.HasValue
                                ? "Fuel"
                                : null)
                        : null));
        }
    }
}
