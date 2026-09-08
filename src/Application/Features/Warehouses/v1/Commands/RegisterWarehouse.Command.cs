using MediatR;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

public class RegisterWarehouseCommand : BaseRequest, IRequest<bool>
{
    public string Code { get; set; } = null!;
    public Guid BranchId { get; set; }
    public bool IsOwner { get; set; } = true;
    public string WarehouseName { get; set; } = null!;
    public WarehouseType WarehouseType { get; set; }
    public Guid? ParentWarehouseId { get; set; }

    // public List<SectionInformation> AssignedZones { get; set; } = [];
}