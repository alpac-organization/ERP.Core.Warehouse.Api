using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using MediatR;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

public class RegisterSectionCommand : BaseRequest, IRequest<bool>
{
    public Guid WarehouseId { get; set; } // se asigna desde la ruta, no viene del body
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public SectionType SectionType { get; set; }
    public decimal WidthMetres { get; set; }
    public decimal LengthMetres { get; set; }

    public SectionOverflowCapacityInformation? OverflowCapacity { get; set; }
}

public class SectionOverflowCapacityInformation
{
    public bool AllowsOverflowStorage { get; set; } = false;
    public bool IsOverflowEnabled { get; set; } = false;
    public int? MaxOverflowPolines { get; set; }
    public string? EnabledByUserName { get; set; }
    public DateOnly? EnabledDate { get; set; }
    public TimeOnly? EnabledTime { get; set; }
}