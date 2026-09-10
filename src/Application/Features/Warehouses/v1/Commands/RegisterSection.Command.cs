using System.Text.Json.Serialization;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using MediatR;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

public class RegisterSectionCommand : BaseRequest, IRequest<bool>
{
    [JsonIgnore]
    public Guid WarehouseId { get; set; }
    public string Code { get; set; } = null!;

    [JsonConverter(typeof(JsonNumberEnumConverter<SectionType>))]
    public SectionType SectionType { get; set; }
    public decimal Width { get; set; }
    public decimal Length { get; set; }
}