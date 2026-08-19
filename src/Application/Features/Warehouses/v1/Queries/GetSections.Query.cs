using MediatR;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries
{
    public class GetSectionsQuery : BaseRequest, IRequest<PagedResponse<SectionDto>>
    {
        public Guid WarehouseId { get; set; }
        public string? SectionCode { get; set; }
        public SectionType? SectionType { get; set; }

        public SectionStorageType? SectionStorageType { get; set; }

        public bool? IsActive { get; set; }

        public int PageSize { get; set; }
        public int PageNumber { get; set; }
    }
}
