using MediatR;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;

public class GetLotByIdQuery : BaseRequest, IRequest<LotDto>
{
    public Guid SectionId { get; set; }
    public Guid LotId { get; set; }
}