using MediatR;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Domain.Entities.Bases;
using System.Text.Json.Serialization;

namespace ERP.Core.Warehouse.Api.Application.Features.Equipments.v1.Commands;

public class PatchEquipmentCommand : BaseRequest, IRequest<Unit>
{
    [JsonIgnore]
    public Guid EquipmentId { get; set; }

    public string? Brand { get; set; }
    public string? Code { get; set; }
    public string? Year { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public Guid? BranchId { get; set; }

    public string? Color { get; set; }
    public EquipmentStatus? Status { get; set; }
    public EquipmentType? Type { get; set; }
    public bool? IsActive { get; set; }

    public bool? Gasoline { get; set; }
    public bool? Diesel { get; set; }
    public bool? Electric { get; set; }
    public bool? Lpg { get; set; }
}