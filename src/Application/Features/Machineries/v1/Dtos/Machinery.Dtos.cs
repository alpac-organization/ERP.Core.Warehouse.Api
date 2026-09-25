using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.Machineries.v1.Dtos;

public class MachineryListDto
{
    public Guid Id { get; set; }
    public string? Brand { get; set; }
    public string? Code { get; set; }
    public MachineryStatus Status { get; set; }
}
