using ERP.Core.Domain.Entities.Bases;
using MediatR;

namespace ERP.Core.Warehouse.Api.Application.Features.OperationalServices.v1.Commands;

public class RegisterOperationalServicesCommand : BaseRequest, IRequest<Unit>
{
    public string ServiceCode { get; set; } = null!;
    public string ServiceName { get; set; } = null!;
    public string Description { get; set; } = null!;
}