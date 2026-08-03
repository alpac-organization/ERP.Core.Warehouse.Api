namespace ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Dtos;

public record CreateDucatRegistryDto
{
    public string ContainerNumber { get; set; } = string.Empty;
    public string Empresa { get; set; } = string.Empty;
    public string? GeneralObservations { get; set; } = string.Empty;
    public bool IsInTransit { get; set; }
}