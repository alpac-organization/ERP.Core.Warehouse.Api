using MediatR;
using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Supplies.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.Supplies.v1.Queries;

public class GetSuppliesQuery : BaseRequest, IRequest<List<SupplyDto>>
{
}
