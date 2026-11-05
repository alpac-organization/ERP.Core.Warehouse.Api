using MediatR;
using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.SupplyRequests.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.SupplyRequests.v1.Queries;

public class GetSupplyRequestsQuery : BaseRequest, IRequest<PagedResponse<GetSupplyRequestDto>>
{
    public string? Code { get; set; }
    public OutflowStatus? Status { get; set; }
    public ReasonUse? ReasonUse { get; set; }
    public Guid? BranchId { get; set; }
    public string? PoCode { get; set; }

    public int PageNumber { get; set; }
    public int PageSize { get; set; }

}