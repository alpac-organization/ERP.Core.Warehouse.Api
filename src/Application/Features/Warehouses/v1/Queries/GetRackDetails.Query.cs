using MediatR;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;

public class GetRackDetailsQuery : BaseRequest, IRequest<RackDetailsDto>
{
    public Guid WarehouseId { get; set; }
    public Guid SectionId { get; set; }
    public Guid RackId { get; set; }
}
