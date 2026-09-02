using System;
using MediatR;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.WarehouseMachineries.v1.Commands
{
    public class CreateWarehouseMachineryCommand : BaseRequest, IRequest<bool>
    {
        public Guid BranchId { get; set; }
        public Guid WarehouseId { get; set; }
        public Guid? AssignedOperatorId { get; set; }
        
        public string Code { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string? LicensePlate { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int ManufactureYear { get; set; }
        
        public MachineryType MachineryType { get; set; }
        public FuelType FuelType { get; set; }
        public decimal LoadCapacityKg { get; set; }
        public decimal? MaxReachHeightMeters { get; set; }
        public decimal HourMeter { get; set; }
        
        public MachineryStatus Status { get; set; }
        public string? Notes { get; set; }
        public DateTime? PurchaseDate { get; set; }
    }
}
