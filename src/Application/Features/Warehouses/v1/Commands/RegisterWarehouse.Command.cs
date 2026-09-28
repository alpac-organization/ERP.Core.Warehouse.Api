using MediatR;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

public class RegisterWarehouseCommand : BaseRequest, IRequest<bool>
{
    public string Code { get; set; } = null!;
    public WarehouseType WarehouseType { get; set; }

    public decimal Width {get; set;}    
    public decimal Length {get; set;}
    public decimal MinimumHeight {get; set;}
    public decimal MaximumHeight {get; set;}
    public bool HasMargins {get; set;}
    public decimal MarginTop {get; set;}
    public decimal MarginBottom {get; set;}
    public decimal MarginLeft {get; set;}
    public decimal MarginRight {get; set;}
    public RegisterWarehouseLocation WarehouseLocation  {get; set;} = null!;
}
public class RegisterWarehouseLocation 
{
   public string LocationName {get; set;} = null!; 
}