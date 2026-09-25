using MediatR;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.Machineries.v1.Commands;

public class MachineryCommand : BaseRequest, IRequest<bool>
{
    public Guid BranchId { get; set; }

    public string Brand { get; set; } = null!;
    public string Code { get; set; } = null!;
    public int Year { get; set; }
    public string Model { get; set; } = null!;
    public string SerialNumber { get; set; } = null!;
}
