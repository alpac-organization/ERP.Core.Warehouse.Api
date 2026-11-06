using MediatR;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Domain.Entities.Bases;

using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.InventoryOutputs.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryOutputs.v1.Queries;

public class GetInventoryOutputsQuery : BaseRequest, IRequest<PagedResponse<GetInventoryOutputDto>>
{
    public string? OutputCode { get; set; }
    public InventoryOutputsStatus? Status { get; set; }
    public string? RequestType { get; set; }
    public string? SupplyRequestCode { get; set; }
    public string? FuelRequestCode { get; set; }
    public Guid? ReasonInventoryRemovalId { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}