using MediatR;
using ERP.Core.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.SupplyRequests.v1.Commands;

public class RegisterSupplyRequestsCommad : BaseRequest, IRequest<Unit>
{
    public string Justification { get; set; } = null!;
    public Guid OperationalOrderId { get; set; }
    public List<RegisterSupplyRequestItem> Items { get; set; } = [];
}

public class RegisterSupplyRequestItem
{
    public Guid ProductStockId { get; set; }
    public int Amount { get; set; }
}