using MediatR;
using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Queries
{
    public class GetMerchandisesQuery : BaseRequest, IRequest<List<MerchandiseDto>>
    {
        public Guid? CategoryProductId { get; set; }
    }
}