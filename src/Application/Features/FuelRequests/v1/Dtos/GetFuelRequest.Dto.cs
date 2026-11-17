using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.FuelRequests.v1.Dtos;

public class GetFuelRequestDto
{
    public Guid FuelRequestId { get; set; }
    public string FuelRequestCode { get; set; } = null!;
    public OutflowStatus OutflowStatus { get; set; }
    public Guid RequestedByUserId { get; set; }
    public string RequestedByUserName { get; set; } = null!;
    public string EquipmentCode { get; set; } = null!;
    public string EquipmentBrand { get; set; } = null!;
    public string EquipmentModel { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}