using MediatR;
using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Unloading.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.Unloading.v1.Queries;

public class GetUnloadingDetailQuery : BaseRequest, IRequest<UnloadingDetailDto>
{
    public Guid UnloadingId { get; set; }
}
