using MediatR;
using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;

public class GetSectionDetailsQuery : BaseRequest, IRequest<SectionDetailsDto>
{
    public Guid WarehouseId { get; set; }
    public Guid SectionId { get; set; }
}
