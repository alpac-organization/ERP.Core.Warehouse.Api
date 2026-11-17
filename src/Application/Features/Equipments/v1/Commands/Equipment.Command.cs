using MediatR;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.Equipments.v1.Commands;

public class EquipmentCommand : BaseRequest, IRequest<Unit>
{
    public Guid BranchId { get; set; }

    public string Brand { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string Year { get; set; } = null!;
    public string Model { get; set; } = null!;
    public string SerialNumber { get; set; } = null!;
    public string? Color { get; set; }
    public EquipmentType Type { get; set; }

    public bool Gasoline { get; set; }
    public bool Diesel { get; set; }
    public bool Electric { get; set; }
    public bool Lpg { get; set; }
}