using ERP.Core.Database.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos
{
    public class PurchaseRequestReturnEntryDto
    {
        public AnnulmentScope Scope { get; set; }

        public string Source { get; set; } = string.Empty;

        public string Reason { get; set; } = string.Empty;

        public DateTime ReturnedAt { get; set; }

        public UserInformation? UserInformation { get; set; }
    }
}
