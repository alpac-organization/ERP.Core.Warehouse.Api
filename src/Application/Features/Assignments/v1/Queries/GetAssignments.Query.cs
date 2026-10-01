using MediatR;
using System.Text.Json.Serialization;

using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Database.Domain.Enums;

using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Queries
{
    public class GetAssignmentsQuery : BaseRequest, IRequest<PagedResponse<AssignmentOperationalDto>>
    {
        [JsonIgnore]
        public Guid OperationalOrderId { get; set; }
        public AssignmentOperationalStatus? Status { get; set; }

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}