namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Dtos;

public record UpdateDucatItemDto
{
    public Guid? Id {get;set;}
    public string DucatNumber {get;set;} = string.Empty;
}
public record UpdateReceptionEntranceDto
{
    public List<UpdateDucatItemDto> Ducats {get; set;} = [];

    public string CountryOfOrigin {get;set;} = string.Empty;
    public string Aduana {get;set;} = string.Empty;
    public string PlateNumber {get;set;} = string.Empty;
    public string TrailerChassis {get;set;} = string.Empty;
    public string DriverLicense {get;set;} = string.Empty;
    public string Transportista {get;set;} = string.Empty;
    public string Medio {get;set;} = string.Empty;
    public string DriverName {get;set;} = string.Empty;
    public string Consignee {get;set;} = string.Empty;
    public string SealNumber {get;set;} = string.Empty;
}