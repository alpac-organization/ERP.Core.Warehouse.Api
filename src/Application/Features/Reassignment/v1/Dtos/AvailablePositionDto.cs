namespace ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Dtos;

public class AvailablePositionDto
{
    public Guid PositionId { get; set; }
    public string PositionCode { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string Status { get; set; } = null!;

    public Guid SectionId { get; set; }
    public string SectionCode { get; set; } = null!;

    public Guid? RackId { get; set; }
    public string? RackCode { get; set; }
    public int? PositionNumber { get; set; }

    public Guid? LotId { get; set; }
    public string? LotCode { get; set; }
    public int? RowNumber { get; set; }
    public int? ColumnNumber { get; set; }

    public Guid? StockId { get; set; }
    public Guid? ReservedBySessionId { get; set; }
}
