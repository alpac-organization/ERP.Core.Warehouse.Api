using MediatR;
using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Unloading.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.Unloading.v1.Queries;

public class GetUnloadingAssignmentDetailQuery : BaseRequest, IRequest<UnloadingAssignmentDetailDto>
{
    public Guid AssignmentId { get; set; }
}