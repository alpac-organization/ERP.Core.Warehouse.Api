using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.Equipments.v1.Dtos;

public class EquipmentDto
{
    public Guid EquipmentId { get; set; }
    public string EquipmentBrand { get; set; } = null!;
    public string EquipmentModel { get; set; } = null!;
    public string EquipmentCode { get; set; } = null!;
    public EquipmentStatus Status { get; set; }
    public EquipmentType Type { get; set; }
    public string? EquipmentColor { get; set; }
    public bool IsActive { get; set; }
    public bool Gasoline { get; set; }
    public bool Diesel { get; set; }
    public bool Electric { get; set; }
    public bool Lpg { get; set; }
    public string BranchCode { get; set; } = null!;

    // Auditoría
    public string? UpdatedByUserName { get; set; }
    public DateTime? UpdatedAt { get; set; }
}