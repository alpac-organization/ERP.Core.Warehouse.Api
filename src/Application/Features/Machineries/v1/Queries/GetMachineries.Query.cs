using MediatR;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Machineries.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.Machineries.v1.Queries
{
    public class GetMachineriesQuery : BaseRequest, IRequest<IEnumerable<MachineryListDto>>
    {
    }
}
