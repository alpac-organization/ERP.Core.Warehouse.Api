using ERP.Core.Database.Application.Commons.Interfaces.Repositories.Warehouse;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using MediatR;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Commands;

public class ExitVehicleCommand : BaseRequest, IRequest<bool>
{
    public Guid ReceptionId { get; set; }
    public bool ExitVehicle { get; set; }
    public bool ExitContainer { get; set; }
    public DateOnly? ExitDate { get; set; }
    public TimeOnly? ExitTime { get; set; }
}