using MediatR;
using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.FuelRequests.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.FuelRequests.v1.Queries;

public class GetFuelRequestsQuery : BaseRequest, IRequest<PagedResponse<GetFuelRequestDto>>
{
    public string? Code { get; set; }
    public OutflowStatus? Status { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? MachineryId { get; set; }
    public Guid? ProductStockId { get; set; }

    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}