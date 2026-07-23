namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Dtos;

public record AddDucatsToReceptionDto
{
    public List<string> DucatNumbers { get; set; } = [];
}