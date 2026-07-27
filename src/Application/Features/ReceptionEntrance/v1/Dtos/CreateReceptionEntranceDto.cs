namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Dtos;

public record CreateReceptionEntranceDto
{
    public List<string> DucatNumbers { get; set; } = [];

    public string CountryOfOrigin { get; set; } = string.Empty;
    public string Aduana { get; set; } = string.Empty;
    public string PlateNumber { get; set; } = string.Empty;
    public string TrailerChassis { get; set; } = string.Empty;
    public string DriverLicense { get; set; } = string.Empty;
    public string Transportista { get; set; } = string.Empty;
    public string Medio { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;
    public string SealNumber { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }
    public TimeOnly StartTime { get; set; }
}