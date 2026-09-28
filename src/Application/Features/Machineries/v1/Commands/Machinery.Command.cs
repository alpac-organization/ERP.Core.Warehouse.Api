using MediatR;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.Machineries.v1.Commands;

public class MachineryCommand : BaseRequest, IRequest<Unit>
{
    public string Brand { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string Year { get; set; } = null!;
    public string Model { get; set; } = null!;
    public string SerialNumber { get; set; } = null!;
    public string? Color { get; set; }
    public MachineryType Type { get; set; }
}
