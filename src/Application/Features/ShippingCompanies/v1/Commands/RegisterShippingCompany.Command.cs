using MediatR;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.ShippingCompanies.v1.Commands;

public class RegisterShippingCompanyCommand : BaseRequest, IRequest<bool>
{
    public string Name { get; set; } = string.Empty;
}