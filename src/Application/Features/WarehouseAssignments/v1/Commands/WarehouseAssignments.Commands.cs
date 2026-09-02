using System;
using MediatR;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Commands
{
    public class CreateWarehouseAssignmentCommand : BaseRequest, IRequest<bool>
    {
        public Guid ReceptionId { get; set; }
        public Guid? EntranceDucatId { get; set; } 
        public Guid WarehouseId { get; set; }
        public Guid WarehouseChiefUserId { get; set; }
    }



    public class CreateUnloadingCrewCommand : BaseRequest, IRequest<bool>
    {
        public Guid ReceptionId { get; set; }
        public Guid? EntranceDucatId { get; set; }
        public List<Guid>? CollaboratorIds { get; set; }
        public bool IsOutsourced { get; set; }
        public int? PersonCount { get; set; }
        public string? ProviderName { get; set; }
        public string? InvoiceNumber { get; set; }
    }

    public class CreateUnloadingMachineryCommand : BaseRequest, IRequest<bool>
    {
        public Guid ReceptionId { get; set; }
        public Guid? EntranceDucatId { get; set; }
        public string MachineryCode { get; set; } = string.Empty;
        public Guid? OperatorCollaboratorId { get; set; }
        public bool IsOutsourced { get; set; }
        public DateTime StartTime { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public string InvoiceNumber { get; set; } = string.Empty;
        public string MachineryDescription { get; set; } = string.Empty;
    }

    public class CompleteWarehouseAssignmentCommand : BaseRequest, IRequest<bool>
    {
        public Guid ReceptionId { get; set; }
        public Guid? EntranceDucatId { get; set; }
    }
}
