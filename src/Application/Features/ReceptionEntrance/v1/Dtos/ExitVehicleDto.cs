namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Dtos;

public record ExitVehicleDto
{
    public bool ExitVehicle { get; set; }
    public bool ExitContainer { get; set; }
    public DateOnly? ExitDate { get; set; }
    public TimeOnly? ExitTime { get; set; }
}