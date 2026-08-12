using MediatR;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries
{
    public class GetSectionsQuery : BaseRequest, IRequest<List<SectionDto>>
    {
        public Guid WarehouseId { get; set; }
    }
}
