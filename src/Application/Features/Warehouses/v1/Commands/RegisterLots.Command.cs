using MediatR;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

public class RegisterLotsCommand : BaseRequest, IRequest<RegisterLotsResultDto>
{
    public Guid SectionId { get; set; }
    public List<RegisterLotGroupDto> Groups { get; set; } = [];
}