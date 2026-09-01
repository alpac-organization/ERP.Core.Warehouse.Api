using MediatR;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Features.Unloading.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.Unloading.v1.Queries;

public class GetAssignmentQueueQuery : BaseRequest, IRequest<GetAssignmentQueueDto>, IPagedQuery
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}