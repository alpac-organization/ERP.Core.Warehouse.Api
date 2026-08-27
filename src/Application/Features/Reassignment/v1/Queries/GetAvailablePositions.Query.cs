using MediatR;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Queries;

public class GetAvailablePositionsQuery : BaseRequest, IRequest<List<AvailablePositionDto>>
{
    public Guid WarehouseId { get; set; }
    public Guid? SectionId { get; set; }
    public string? Status { get; set; }
}
