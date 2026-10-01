using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.OperationalOrders.v1.Dtos
{
    public class OperationalOrderDto
    {
        public Guid OperationOrderId { get; set; }
        public string? PoCode { get; set; }
        public string? DocumentNumber { get; set; }
        public OperationalOrderStatus Status { get; set; }
        public bool IsAlerted { get; set; }

        public CustomerInformation? CustomerInformation { get; set; }
        public CostCenterInformation? CostCenterInformation { get; set; }
    }
}