using MediatR;
using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.SupplyRequests.v1.Commands;

public class RegisterSupplyRequestsCommad : BaseRequest, IRequest<string>
{
    public string Justification { get; set; } = null!;
    public Guid OperationalOrderId { get; set; }
    public List<RegisterSupplyRequestItem> Items { get; set; } = [];
    public ReasonUse ReasonUse { get; set; }
}

public class RegisterSupplyRequestItem
{
    public Guid ProductStockId { get; set; }
    public decimal Amount { get; set; }
}

// ============================================================
// FLUJO PARALELO TEMPORAL - SE ELIMINARÁ EN EL FUTURO
// Segunda opción de registro: el usuario ingresa manualmente
// Equipment, Customer, DucaOrTransportOrder y Warehouse (sin derivación).
// ============================================================
public class RegisterSupplyRequestManualCommand : BaseRequest, IRequest<string>
{
    public string Justification { get; set; } = null!;
    public List<RegisterSupplyRequestItem> Items { get; set; } = [];
    public ReasonUse ReasonUse { get; set; }

    // Campos manuales (sin derivación)
    public Guid? EquipmentId { get; set; }
    public Guid? CustomerId { get; set; }
    public string? DucaOrTransportOrder { get; set; }
    public Guid WarehouseId { get; set; }
}