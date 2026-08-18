using ERP.Core.Warehouse.Api.Application.Features.ShippingCompanies.v1.Dtos;
using MediatR;

namespace ERP.Core.Warehouse.Api.Application.Features.ShippingCompanies.v1.Queries;

public class GetShippingCompaniesQuery : IRequest<List<ShippingCompanyDto>>
{
    public Guid CompanyId { get; set; }
    public string ModuleCode { get; set; } = string.Empty;
    public Guid UserId { get; set; }
}