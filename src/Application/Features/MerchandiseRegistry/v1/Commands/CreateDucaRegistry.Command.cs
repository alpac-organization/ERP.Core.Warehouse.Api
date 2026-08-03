using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using MediatR;

namespace ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Commands;

public class CreateDucatRegistryCommand : BaseRequest, IRequest<bool>
{
    public Guid ReceptionId { get; set; }
    public string ContainerNumber { get; set; } = string.Empty;
    public string Empresa { get; set; } = string.Empty;
    public string? GeneralObservations { get; set; } = string.Empty;
    public bool IsInTransit { get; set; }
    public DateOnly? RegisteredStartDate { get; set; }
    public TimeOnly? RegisteredStartTime { get; set; }

}