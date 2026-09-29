using MediatR;
using System.Text.Json.Serialization;

using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.ServiceOrder.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.ServiceOrder.v1.Queries
{
    public class GetServiceOrderRequisitionsQuery : BaseRequest, IRequest<PagedResponse<ServiceOrderRequisitionDto>>
    {   
        [JsonIgnore]
        public Guid ServiceOrderId { get; set; }

        [JsonIgnore]
        public Guid OperationalOrderId { get; set; }

        public int PageSize { get; set; }
        public int PageNumber { get; set; }
    }
}
