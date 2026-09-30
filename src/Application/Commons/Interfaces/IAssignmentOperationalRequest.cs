namespace ERP.Core.Warehouse.Api.Application.Commons.Interfaces;

public interface IAssignmentOperationalRequest
{
    Guid CompanyId { get; set; }
    string ModuleCode { get; set; }
    Guid UserId { get; set; }
    Guid OperationalOrderId { get; set; }
}
