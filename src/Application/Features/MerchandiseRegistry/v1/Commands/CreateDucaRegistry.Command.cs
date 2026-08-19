using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using MediatR;

namespace ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Commands;

public class CreateDucatRegistryCommand : BaseRequest, IRequest<bool>
{
    public Guid ReceptionId { get; set; }
    public Guid ShippingCompanyId { get; set; }
    public string? GeneralObservations { get; set; } = string.Empty;
    public bool IsInTransit { get; set; }
    public DateOnly? RegisteredStartDate { get; set; }
    public TimeOnly? RegisteredStartTime { get; set; }
}

public class CreateDucatRegistryDetailCommand : BaseRequest, IRequest<bool>
{
    public Guid ReceptionId { get; set; }
    public Guid EntranceDucatId { get; set; }
    public Guid ServiceOrderId { get; set; }

    public Guid MerchandiseId { get; set; }
    public DucaType Type { get; set; }
    public int TotalBultos { get; set; }
    public decimal TotalWeight { get; set; }
    public string? MerchandiseDescription { get; set; }
    public string Sender { get; set; } = string.Empty;
    public string? DestinationAreaObservation { get; set; }
    public DateOnly? RegisteredStartDate { get; set; }
    public TimeOnly? RegisteredStartTime { get; set; }
}

public class AssignServiceOrderToCustomsDeclarationCommand : BaseRequest, IRequest<bool>
{
    public Guid ReceptionId { get; set; }
    public Guid ServiceOrderId { get; set; }
}