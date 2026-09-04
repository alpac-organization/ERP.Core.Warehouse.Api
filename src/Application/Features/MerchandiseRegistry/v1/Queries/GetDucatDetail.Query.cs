using ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Dtos;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using MediatR;

namespace ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Queries;

public class GetDucatDetailQuery : BaseRequest, IRequest<GetDucatDetailDto>
{
    public Guid DucatId { get; set; }
}
