using MediatR;
using System.Text.Json.Serialization;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.RequisitionAccountingReviews.v1.Commands
{
    public class AnnulAccountingReviewCommand : BaseRequest, IRequest<bool>
    {
        [JsonIgnore]
        public Guid RequisitionAccountingReviewId { get; set; }

        public AnnulmentScope Scope { get; set; }

        public string Reason { get; set; } = default!;
    }
}
