using MediatR;
using ERP.Core.Domain.Entities.Bases;
using System.Text.Json.Serialization;
using ERP.Core.Database.Domain.Enums;


namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands
{
    public class UpdateAssignmentCommand : BaseRequest, IRequest<Unit>
    {
        [JsonIgnore]
        public Guid AssignmentId { get; set; }

        [JsonIgnore]
        public Guid OperationalOrderId { get; set; }

        public string? Observations { get; set; }
        public string? Merchandise { get; set; }
        public string? MerchandiseDescription { get; set; }

        public Guid? WarehouseId { get; set; }
        public DestinationType? DestinationType { get; set; }
    }
}