using MediatR;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;

public class GetSectionDetailsQuery : BaseRequest, IRequest<SectionDetailsDto>
{
    public Guid WarehouseId { get; set; }
    public Guid SectionId { get; set; }
}
