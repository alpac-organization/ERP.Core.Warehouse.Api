namespace ERP.Core.Warehouse.Api.Application.Features.OperationalServices.v1.Dtos;

public class GetOperationalServiceDto
{
    public string ServiceCode { get; set; } = default!;
    public string ServiceName { get; set; } = default!;
    public string Description { get; set; } = default!;
}