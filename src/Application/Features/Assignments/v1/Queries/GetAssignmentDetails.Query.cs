using MediatR;
using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Queries
{
    public class GetAssignmentDetailsQuery : BaseRequest, IRequest<AssignmentDetailsDto>
    {
        public Guid AssignmentId { get; set; }
    }
}