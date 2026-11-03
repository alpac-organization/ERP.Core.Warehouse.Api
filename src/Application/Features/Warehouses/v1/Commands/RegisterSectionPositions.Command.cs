using MediatR;
using System.Text.Json.Serialization;
using ERP.Core.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

public class RegisterSectionPositionsCommand : BaseRequest, IRequest<bool>
{
    [JsonIgnore]
    public Guid WarehouseId { get; set; }

    [JsonIgnore]
    public Guid SectionId { get; set; }

    public int Rows { get; set; }

    public int Columns { get; set; }

    public int Levels { get; set; }
}
