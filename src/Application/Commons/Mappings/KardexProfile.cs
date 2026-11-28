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
                .ForMember(dest => dest.ReferenceType, opt => opt.MapFrom(src => ResolveReferenceType(src)))
                .ForMember(dest => dest.DocumentNumber, opt => opt.MapFrom(src => ResolveDocumentNumber(src)))
                .ForMember(dest => dest.InventoryEntryCode, opt => opt.MapFrom(src =>
                    src.InventoryEntry != null ? src.InventoryEntry.Code : null))
                .ForMember(dest => dest.InvoiceNumber, opt => opt.MapFrom(src =>
                    src.InventoryEntry != null ? src.InventoryEntry.InvoiceNumber : null))
                .ForMember(dest => dest.PurchaseRequestCode, opt => opt.MapFrom(src =>
                    src.InventoryEntry != null &&
                    src.InventoryEntry.PurchaseOrder != null &&
                    src.InventoryEntry.PurchaseOrder.PurchaseRequest != null
                        ? src.InventoryEntry.PurchaseOrder.PurchaseRequest.Code
                        : null))
                .ForMember(dest => dest.InventoryOutputCode, opt => opt.MapFrom(src =>
                    src.InventoryOutput != null ? src.InventoryOutput.Code : null))
                .ForMember(dest => dest.OutputRequestCode, opt => opt.MapFrom(src =>
                    ResolveOutputRequestCode(src)))
                .ForMember(dest => dest.OutputRequestType, opt => opt.MapFrom(src =>
                    src.InventoryOutput != null
                        ? (src.InventoryOutput.SupplyRequestId.HasValue
                            ? "Supply"
                            : src.InventoryOutput.FuelRequestId.HasValue
                                ? "Fuel"
                                : null)
                        : null))
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
                .ForMember(dest => dest.InvoiceDate, opt => opt.MapFrom(src =>
                    src.InventoryEntry != null ? src.InventoryEntry.InvoiceDate : null))
                .ForMember(dest => dest.InventoryEntryStatus, opt => opt.MapFrom(src =>
                    src.InventoryEntry != null ? (EntryStatus?)src.InventoryEntry.EntryStatus : null))
                .ForMember(dest => dest.EntryObservations, opt => opt.MapFrom(src =>
                    src.InventoryEntry != null ? src.InventoryEntry.Observations : null))
                .ForMember(dest => dest.PurchaseOrderCode, opt => opt.MapFrom(src =>
                    src.InventoryEntry != null && src.InventoryEntry.PurchaseOrder != null
                        ? src.InventoryEntry.PurchaseOrder.Code
                        : null))
                .ForMember(dest => dest.SupplierName, opt => opt.MapFrom(src =>
                    src.InventoryEntry != null &&
                    src.InventoryEntry.PurchaseOrder != null &&
                    src.InventoryEntry.PurchaseOrder.Supplier != null
                        ? (src.InventoryEntry.PurchaseOrder.Supplier.CommercialName ??
                           src.InventoryEntry.PurchaseOrder.Supplier.SuppliersLegalName)
                        : null))
                .ForMember(dest => dest.InventoryOutputStatus, opt => opt.MapFrom(src =>
                    src.InventoryOutput != null ? (InventoryOutputsStatus?)src.InventoryOutput.Status : null))
                .ForMember(dest => dest.ReasonRemovalDescription, opt => opt.MapFrom(src =>
                    src.InventoryOutput != null && src.InventoryOutput.ReasonsInventoryRemoval != null
                        ? src.InventoryOutput.ReasonsInventoryRemoval.Description
                        : null));
        }

        private static ReferenceType ResolveReferenceType(Kardex src)
        {
            if (src.ReferenceType is ReferenceType.FT or ReferenceType.BL or ReferenceType.RQ)
            {
                return src.ReferenceType;
            }

            if (src.InventoryOutput?.FuelRequestId.HasValue == true)
            {
                return ReferenceType.BL;
            }

            if (src.InventoryOutput?.SupplyRequestId.HasValue == true)
            {
                return ReferenceType.RQ;
            }

            if (src.InventoryEntryId.HasValue || src.MovementType == KardexMovementType.Buys)
            {
                return ReferenceType.FT;
            }

            return ReferenceType.None;
        }

        private static string? ResolveOutputRequestCode(Kardex src)
            => src.InventoryOutput?.SupplyRequest?.Code
               ?? src.InventoryOutput?.FuelRequest?.Code;

        private static string? ResolveDocumentNumber(Kardex src)
        {
            return ResolveReferenceType(src) switch
            {
                ReferenceType.BL => src.InventoryOutput?.FuelRequest?.Code ?? src.InventoryOutput?.Code,
                ReferenceType.RQ => src.InventoryOutput?.SupplyRequest?.Code ?? src.InventoryOutput?.Code,
                ReferenceType.FT => src.InventoryEntry?.PurchaseOrder?.PurchaseRequest?.Code
                    ?? src.InventoryEntry?.Code,
                _ => src.InventoryOutput?.Code ?? src.InventoryEntry?.Code
            };
        }
    }
}
