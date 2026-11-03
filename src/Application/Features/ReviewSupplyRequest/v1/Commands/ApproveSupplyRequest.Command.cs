using MediatR;
using ERP.Core.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.ReviewSupplyRequest.v1.Commands;

public class ApproveSupplyRequestCommand : BaseRequest, IRequest<Unit>
{
    public string? Observations { get; set; }
    public Guid BranchId { get; set; }
    public Guid WorkAreaId { get; set; }
    public Guid CostCenterId { get; set; }
    public Guid SupplyRequestId { get; set; }
    public Guid ReasonInventoryRemovalId { get; set; }
}