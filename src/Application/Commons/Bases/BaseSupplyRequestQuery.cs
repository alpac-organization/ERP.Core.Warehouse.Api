using ERP.Core.Database.Domain.Enums;
using ERP.Core.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Commons.Bases;

public abstract class BaseSupplyRequestQuery : BaseRequest
{
    public string? Code { get; set; }
    public OutflowStatus? Status { get; set; }
    public Guid? BranchId { get; set; }

    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}