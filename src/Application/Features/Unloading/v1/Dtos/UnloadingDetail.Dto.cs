using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.Unloading.v1.Dtos;

public class UnloadingDetailDto
{
    public Guid AssignmentId { get; set; }
    public Guid RecordEntranceId { get; set; }
    public Guid? UnloadingDetailsId { get; set; }
    public UnloadingStatus UnloadingStatus { get; set; }
    public DateOnly? StartDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public UnloadingMerchandiseType? MerchandiseType { get; set; }
    public List<UnloadingPalletDetailDto> Pallets { get; set; } = [];
    public List<UnloadingSupplyDetailDto> Supplies { get; set; } = [];
    public List<UnloadingPositionReservationDetailDto> ReservedPositions { get; set; } = [];
}

public class UnloadingPalletDetailDto
{
    public PalletType Type { get; set; }
    public int Quantity { get; set; }
    public decimal? LengthMetres { get; set; }
    public decimal? WidthMetres { get; set; }
}

public class UnloadingSupplyDetailDto
{
    public string? SupplyName { get; set; }
    public decimal Quantity { get; set; }
}

public class UnloadingPositionReservationDetailDto
{
    public Guid? RackPositionId { get; set; }
    public Guid? LotPositionId { get; set; }
    public string? PositionCode { get; set; }
}
