using MediatR;
using ERP.Core.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Commands;

public class ExitVehicleCommand : BaseRequest, IRequest<bool>
{
    public Guid ReceptionId { get; set; }
    public bool ExitVehicle { get; set; }
    public bool ExitContainer { get; set; }
    public DateOnly? ExitDate { get; set; }
    public TimeOnly? ExitTime { get; set; }
}