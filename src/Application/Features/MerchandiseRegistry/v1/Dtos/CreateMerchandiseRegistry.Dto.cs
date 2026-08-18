namespace ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Dtos;

public record CreateDucatRegistryDto
{
    public Guid ShippingCompanyId { get; set; }
    public string? GeneralObservations { get; set; } = string.Empty;
    public bool IsInTransit { get; set; }
    public DateOnly? RegisteredStartDate { get; set; }
    public TimeOnly? RegisteredStartTime { get; set; }
}

public record CreateDucatRegistryDetailDto
{
    public Guid? ServiceOrderId { get; set; }
    public Guid MerchandiseId { get; set; }
    public int TotalBultos { get; set; }
    public decimal TotalWeight { get; set; }
    public string? ProductDescription { get; set; }
    public string Remitente { get; set; } = string.Empty;
    public string? DestinationAreaObservation { get; set; }
    public DateOnly? RegisteredStartDate { get; set; }
    public TimeOnly? RegisteredStartTime { get; set; }
}

public record AssignServiceOrderToCustomsDeclarationDto
{
    public Guid ServiceOrderId { get; set; }
}