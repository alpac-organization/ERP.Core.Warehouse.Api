using ERP.Core.Database.Domain.Enums;
using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using MediatR;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;

public class GetPositionsQuery : BaseRequest, IRequest<GetPositionsDto>
{
    public Guid WarehouseId { get; set; }
    public Guid SectionId { get; set; }
    public Guid? TramoId { get; set; }
    public Guid? RackId { get; set; }
    public RackStatus? Status { get; set; }
}
