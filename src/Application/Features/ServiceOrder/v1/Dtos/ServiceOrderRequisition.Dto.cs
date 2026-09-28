using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.ServiceOrder.v1.Dtos
{
    public class ServiceOrderRequisitionDto
    {
        public string? Concept { get; set; }
        public string? SoRequitionCode { get; set; }
        public string? ServiceOrderCode { get; set; }
        
        public Guid ServiceOrderRequisitionId { get; set; }
        public ServiceOrderRequisitionStatus Status { get; set; }

        public UserInformation CreatedByUserInformation { get; set; } = new();       
    }
}