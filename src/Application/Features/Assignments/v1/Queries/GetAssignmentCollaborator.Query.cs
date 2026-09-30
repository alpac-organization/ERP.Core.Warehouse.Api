using MediatR;
using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Dtos;

using ERP.Core.Warehouse.Api.Application.Commons.Interfaces;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Queries;

public class GetAssignmentCollaboratorsQuery : BaseRequest, IRequest<PagedResponse<GetAssignmentCollaboratorsDto>>, IAssignmentOperationalRequest
{
    public Guid OperationalOrderId { get; set; }
    public Guid AssignmentId { get; set; }
    public int PageSize { get; set; }
    public int PageNumber { get; set; }
}