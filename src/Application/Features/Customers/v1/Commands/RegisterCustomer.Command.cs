using ERP.Core.Database.Domain.Enums;
using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Customers.v1.Dtos;
using MediatR;

namespace ERP.Core.Warehouse.Api.Application.Features.Customers.v1.Commands;

public class RegisterCustomerCommand : BaseRequest, IRequest<RegisterCustomerResult>
{
    public string LegalName { get; set; } = null!;
    public string IdentificationNumber { get; set; } = null!;

    public CustomerType CustomerType { get; set; }
    public IdentificationType IdentificationType { get; set; }

}