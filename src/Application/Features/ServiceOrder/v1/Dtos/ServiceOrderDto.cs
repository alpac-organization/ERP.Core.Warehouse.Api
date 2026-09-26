using ERP.Core.Database.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.ServiceOrder.v1.Dtos
{
    public class ServiceOrderDto
    {
        public Guid ServiceOrderId { get; set; }
        public string Code { get; set; } = null!;
        public string? Observations { get; set; }

        public CustomerInformation? Customer { get; set; }
    }    
}
