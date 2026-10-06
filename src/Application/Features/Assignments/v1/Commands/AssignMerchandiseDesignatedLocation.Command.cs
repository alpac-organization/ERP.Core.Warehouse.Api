using MediatR;
using System.Text.Json.Serialization;
using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands
{
    public class AssignMerchandiseDesignatedLocationCommand : BaseRequest, IRequest<AssignMerchandiseDesignatedLocationDto>
    {
        [JsonIgnore]
        public  Guid OperationalOrderId { get; set; }

        [JsonIgnore]
        public Guid AssignmentOperationalId { get; set; }
        
    }
}