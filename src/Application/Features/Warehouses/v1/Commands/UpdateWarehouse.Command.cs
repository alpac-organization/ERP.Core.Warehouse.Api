using System.Text.Json.Serialization;
using MediatR;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

public class UpdateWarehouseCommand : BaseRequest, IRequest<bool>
{
    [JsonIgnore]
    public Guid WarehouseId { get; set; }

    public string? Code { get; set; }
    public bool? IsActive { get; set; }
    public WarehouseType? WarehouseType { get; set; }

    public UpdateWarehouseLocationDto? Location { get; set; }
    public UpdateWarehouseCapacityDto? Capacity { get; set; }
}

public class UpdateWarehouseLocationDto
{
    public string? LocationName { get; set; }
}

public class UpdateWarehouseCapacityDto
{
    public decimal? Width { get; set; }
    public decimal? Length { get; set; }
    public bool? HasMargins { get; set; }
    public decimal? MinimumHeight { get; set; }
    public decimal? MaximumHeight { get; set; }
    public decimal? MarginTop { get; set; }
    public decimal? MarginBottom { get; set; }
    public decimal? MarginRight { get; set; }
    public decimal? MarginLeft { get; set; }
}
