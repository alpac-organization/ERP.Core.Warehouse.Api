using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;

public class RackListDto
{
    public Guid RackId { get; set; }
    public string Code { get; set; } = string.Empty;
    public Guid SectionId { get; set; }
    public int RowNumber { get; set; }
    public int LevelNumber { get; set; }
    public int MaxPulleys { get; set; }
    public RackStatus Status { get; set; }
    public RackUsageProfile UsageProfile { get; set; }

    // Medidas físicas para el plano 2D
    public decimal Width { get; set; }
    public decimal Length { get; set; }
    public decimal? Height { get; set; }

    // Coordenadas para el plano 2D
    public decimal PositionX { get; set; }
    public decimal PositionY { get; set; }
    public decimal RotationY { get; set; }

    // Métricas de posiciones
    public int TotalPositions { get; set; }
    public int OccupiedPositions { get; set; }
    public int AvailablePositions { get; set; }
}