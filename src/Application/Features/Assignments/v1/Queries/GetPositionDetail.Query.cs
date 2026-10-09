using MediatR;
using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Queries
{
    public class GetPositionDetailQuery : BaseRequest, IRequest<PositionDetailDto>
    {
        public Guid PositionId { get; set; }
    }
}