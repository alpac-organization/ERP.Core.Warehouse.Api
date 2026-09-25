using MediatR;
using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Queries;

public class GetDucatDetailQuery : BaseRequest, IRequest<GetDucatDetailDto>
{
    public Guid DucatId { get; set; }
}
