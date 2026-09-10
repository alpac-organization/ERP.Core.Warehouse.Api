using MediatR;
using System.Text.Json.Serialization;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

public class UpdateSectionCommand : BaseRequest, IRequest<bool>
{
    [JsonIgnore]
    public Guid WarehouseId { get; set; }
    [JsonIgnore]
    public Guid SectionId { get; set; }
    public string? Code { get; set; }
    public SectionType? SectionType { get; set; }
    public decimal? Width { get; set; }
    public decimal? Length { get; set; }
}
