using MediatR;
using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.OperationalServices.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.OperationalServices.v1.Queries;

public class GetOperationalServicesQuery : BaseRequest, IRequest<PagedResponse<GetOperationalServiceDto>>
{
    public int PageSize { get; set; }
    public int PageNumber { get; set; }
    public string? ServiceCode { get; set; }
    public string? ServiceName { get; set; }
}