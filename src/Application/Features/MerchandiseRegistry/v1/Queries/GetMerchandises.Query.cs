using ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Dtos;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using MediatR;

namespace ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Queries;

public class GetMerchandisesQuery : BaseRequest, IRequest<List<MerchandiseDto>>
{
    public Guid? CategoryProductId { get; set; }
}