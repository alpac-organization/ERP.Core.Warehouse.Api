using MediatR;
using ERP.Core.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.FuelRequests.v1.Commands;

public class RegisterFuelRequestsCommand : BaseRequest, IRequest<string>
{
    public string Justification { get; set; } = null!;
    public Guid EquipmentId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid ProductStockId { get; set; }
    public decimal RequestedQuantity { get; set; }
    public decimal? Mileage { get; set; }                           // kilometraje del vehiculo particular
    public decimal? EngineHours { get; set; }

    public Guid? CustomerId { get; set; }                           // Para corinto
}