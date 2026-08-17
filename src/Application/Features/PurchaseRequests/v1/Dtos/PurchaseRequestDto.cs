using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos
{
    public class PurchaseRequestDto
    {
        public string? Code { get; set; }
        public Guid PurchaseRequestId { get; set; }
        public DateOnly RequestDate { get; set; }
        public DateOnly? RevisionDate { get; set; }

        public PriorityLevel PriorityLevel { get; set; }
        public DestinationRequest Destination { get; set; }
        public PurchaseRequestType RequestType { get; set; }
        public PurchaseRequestStatus RequestStatus { get; set; }
    }
}
