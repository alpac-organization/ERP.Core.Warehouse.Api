namespace ERP.Core.Warehouse.Api.Application.Features.ShippingCompanies.v1.Dtos;

public record RegisterShippingCompanyDto
{
    public string Name { get; set; } = string.Empty;
}