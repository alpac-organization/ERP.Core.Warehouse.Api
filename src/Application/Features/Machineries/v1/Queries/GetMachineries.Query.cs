using MediatR;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Machineries.v1.Dtos;
using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.Machineries.v1.Queries
{
    public class GetMachineriesQuery : BaseRequest, IRequest<PagedResponse<MachineryDto>>
    {
        public MachineryType? Type { get; set; }
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
    }
}
