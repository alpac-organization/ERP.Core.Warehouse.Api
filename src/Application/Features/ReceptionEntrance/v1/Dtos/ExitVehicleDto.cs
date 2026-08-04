namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Dtos;

public record ExitVehicleDto
{
    public DateOnly? ExitDate {get;set;}
    public TimeOnly? ExitTime {get;set;}
}