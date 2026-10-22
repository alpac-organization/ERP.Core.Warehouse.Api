namespace ERP.Core.Warehouse.Api.Application.Features.Customers.v1.Dtos;

public class RegisterCustomerResult
{
    public Guid CustomerId { get; set; }
    public string? CIFCode { get; set; }
}