using MediatR;
using ERP.Core.Database.Domain.Enums;
using System.Text.Json.Serialization;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.Unloading.v1.Commands;

public class StartUnloadingCommand : BaseRequest, IRequest<bool>
{
    [JsonIgnore]
    public Guid AssignmentId { get; set; }

    public DateOnly? StartDate { get; set; }
    public TimeOnly? StartTime { get; set; }

    public UnloadingMerchandiseType MerchandiseType { get; set; }
    public List<StartUnloadingPalletItem> Pallets { get; set; } = [];
    public List<StartUnloadingSupplyItem> Supplies { get; set; } = [];
}

public class StartUnloadingPalletItem
{
    public PalletType Type { get; set; }
    public int Quantity { get; set; }
    public decimal? LengthMetres { get; set; }
    public decimal? WidthMetres { get; set; }
}

public class StartUnloadingSupplyItem
{
    public Guid SuppliesId { get; set; }
    public decimal Quantity { get; set; }
}
