using ERP.Core.Warehouse.Api.Application.Features.FuelRequests.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.SupplyRequests.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryOutputs.v1.Dtos;

public class GetInventoryOutputDetailDto
{
    public GetSupplyRequestDetailDto? SupplyRequestDetail { get; set; }
    public GetFuelRequestDetailDto? FuelRequestDetail { get; set; }
    // public InventoryStockInformation InventoryStockInformation { get; set; }
}