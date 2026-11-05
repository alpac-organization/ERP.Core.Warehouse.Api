using MediatR;
using ERP.Core.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.FuelRequests.v1.Commands;

public class RegisterFuelRequestsCommand : BaseRequest, IRequest<Unit>
{
    public string Justification { get; set; } = null!;
    public Guid MachineryId { get; set; }
    public Guid ProductStockId { get; set; }
    public decimal RequestedQuantity { get; set; }
}