using MediatR;
using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.InventoryOutputs.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryOutputs.v1.Queries;

public class GetInventoryOutputsDetailQuery : BaseRequest, IRequest<GetInventoryOutputDetailDto>
{
    public Guid InventoryOutputId { get; set; }
}