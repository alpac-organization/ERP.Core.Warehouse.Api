using MediatR;
using System.Text.Json.Serialization;
using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.RequisitionManagementReviews.v1.Commands
{
    public class AnnulManagementReviewCommand : BaseRequest, IRequest<bool>
    {
        [JsonIgnore]
        public Guid RequisitionManagementReviewId { get; set; }

        public AnnulmentScope Scope { get; set; }

        public string Reason { get; set; } = default!;
    }
}
