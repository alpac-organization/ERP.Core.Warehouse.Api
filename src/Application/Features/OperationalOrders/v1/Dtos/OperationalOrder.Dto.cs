using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.ServiceOrder.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.OperationalOrders.v1.Dtos
{
    public class OperationalOrderDto
    {
        public Guid OperationOrderId { get; set; }
        
        public string? OpCode { get; set; }
        public string? DocumentNumber { get; set; }

        public OperationalOrderStatus Status { get; set; }

        public CustomerInformation CustomerInformation { get; set; } = new();
        public CostCenterInformation CostCenterInformation { get; set; } = new();
    }
}