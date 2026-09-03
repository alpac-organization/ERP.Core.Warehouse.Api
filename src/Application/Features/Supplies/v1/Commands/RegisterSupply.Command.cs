using MediatR;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.Supplies.v1.Commands;

public class RegisterSupplyCommand : BaseRequest, IRequest<bool>
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
