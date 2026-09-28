using ERP.Core.Database.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.ServiceOrder.v1.Dtos
{
    public class ServiceOrderDto
    {
        public Guid ServiceOrderId { get; set; }
        public string? Concept { get; set; }
        public string? ServiceOrderCode { get; set; }

        //Mapear OperationalServiceInformation
        public UserInformation CreatedUserInformation { get; set; }
    }
}
