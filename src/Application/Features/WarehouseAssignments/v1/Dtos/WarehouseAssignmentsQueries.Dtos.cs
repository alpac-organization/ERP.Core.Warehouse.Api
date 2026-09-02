using System;
using System.Collections.Generic;

namespace ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Dtos
{
    public class PendingWarehouseAssignmentDto
    {
        public Guid ReceptionId { get; set; }
        public string LicensePlate { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;
        public DateTime EntranceTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsConsolidated { get; set; }

        public List<PendingDucaDto> Ducas { get; set; } = new();
    }

    public class PendingDucaDto
    {
        public Guid EntranceDucatId { get; set; }
        public string DucatNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? ServiceOrderCode { get; set; }
        public bool AlreadyAssigned { get; set; }  
    }

    public class WarehouseStaffDto
    {
        public Guid UserId { get; set; }
        public string Fullname { get; set; } = string.Empty;
    }

    public class WarehouseAssignmentDetailDto
    {
        public Guid ReceptionId { get; set; }
        public Guid? AssignmentId { get; set; }
        public string LicensePlate { get; set; } = string.Empty;
        public string? WarehouseName { get; set; }

        // Informacion del documento vinculado
        public string? DucatNumber { get; set; }        // poblado si es DUCA
        public string? ServiceOrderCode { get; set; }   // OS de la DUCA (si aplica)

        public DateTime? UnloadingStartTime { get; set; }
        public DateTime? UnloadingEndTime { get; set; }

        public List<WarehouseCrewGroupDto> Crews { get; set; } = new();
        public List<WarehouseMachineryDetailDto> Machineries { get; set; } = new();
    }

    public class WarehouseCrewGroupDto
    {
        public bool IsOutsourced { get; set; }
        public string? ProviderName { get; set; }
        public string? InvoiceNumber { get; set; }
        public int TotalPersonCount { get; set; }
        public List<Guid> CollaboratorIds { get; set; } = new();
        public List<string> CollaboratorNames { get; set; } = new();
        public List<Guid> CrewAssignmentIds { get; set; } = new();
    }

    public class WarehouseMachineryDetailDto
    {
        public Guid MachineryAssignmentId { get; set; }
        public bool IsOutsourced { get; set; }

        // Datos para maquinaria interna
        public Guid? MachineryId { get; set; }
        public string? MachineryCode { get; set; }
        public string? MachineryName { get; set; }

        // Datos del operador
        public Guid? OperatorCollaboratorId { get; set; }
        public string? OperatorName { get; set; }

        // Datos para maquinaria tercerizada
        public string? ProviderName { get; set; }
        public string? InvoiceNumber { get; set; }
        public string? MachineryDescription { get; set; }

        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }
}
