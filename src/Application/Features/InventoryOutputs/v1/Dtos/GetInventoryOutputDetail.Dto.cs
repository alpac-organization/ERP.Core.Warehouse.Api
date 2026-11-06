using ERP.Core.Database.Domain.Entities.Bases;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.FuelRequests.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.SupplyRequests.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryOutputs.v1.Dtos;

public class GetInventoryOutputDetailDto
{
    public GetSupplyRequestDetailDto? SupplyRequestDetail { get; set; }
    public GetFuelRequestDetailDto? FuelRequestDetail { get; set; }

    public List<InventoryStockInformation> InventoryStockInformations { get; set; } = [];
    public string? DispatchedByUserName { get; set; }
    public DateTime? DispatchedAt { get; set; }
    public string? DispatchedJustification { get; set; }

}