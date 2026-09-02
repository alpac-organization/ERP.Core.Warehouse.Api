using System;
using System.Collections.Generic;

namespace ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Dtos
{
    public class AssignWarehouseDto
    {
        public Guid? EntranceDucatId { get; set; }
        public Guid WarehouseId { get; set; }
        public Guid WarehouseChiefUserId { get; set; }
    }

    public class AssignUnloadingCrewDto
    {
        public Guid? EntranceDucatId { get; set; }
        public List<Guid>? CollaboratorIds { get; set; }
        public bool IsOutsourced { get; set; }
        public int? PersonCount { get; set; }
        public string? ProviderName { get; set; }
        public string? InvoiceNumber { get; set; }
    }

    public class AssignUnloadingMachineryDto
    {
        public Guid? EntranceDucatId { get; set; }
        public string? MachineryCode { get; set; }
        public Guid? OperatorCollaboratorId { get; set; }
        public bool IsOutsourced { get; set; }
        public DateTime StartTime { get; set; }
        public string? ProviderName { get; set; }
        public string? InvoiceNumber { get; set; }
        public string? MachineryDescription { get; set; }
    }

    public class CompleteWarehouseAssignmentDto
    {
        public Guid? EntranceDucatId { get; set; }
    }

}

