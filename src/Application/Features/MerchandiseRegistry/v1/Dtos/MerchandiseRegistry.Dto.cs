using ERP.Core.Manager.Api.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Dtos;

public class MerchandiseRegistryListItemDto
{
    public Guid Id { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;
    public string? ContainerNumber { get; set; }
    public DateOnly ArrivalDate { get; set; }
    public TimeOnly ArrivalTime { get; set; }
    public DocumentType DocumentType { get; set; }
    public int TotalDocuments { get; set; }
    public int CompletedDocuments { get; set; }
}

public class GetMerchandiseRegistryDto
{
    public List<MerchandiseRegistryListItemDto> Data { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
}