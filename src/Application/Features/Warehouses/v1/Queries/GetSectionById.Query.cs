using MediatR;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;

public class GetSectionByIdQuery : BaseRequest, IRequest<SectionDto>
{
    public Guid WarehouseId { get; set; }
    public Guid SectionId { get; set; }
}
