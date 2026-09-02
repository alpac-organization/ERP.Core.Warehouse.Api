namespace ERP.Core.Warehouse.Api.Application.Features.Supplies.v1.Dtos;

public record RegisterSupplyDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
