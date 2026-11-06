using MediatR;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.SupplyRequests.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.SupplyRequests.v1.Queries;

public class GetSupplyRequestsQuery : BaseSupplyRequestQuery, IRequest<PagedResponse<GetSupplyRequestDto>>
{
    public ReasonUse? ReasonUse { get; set; }
    public string? PoCode { get; set; }
}