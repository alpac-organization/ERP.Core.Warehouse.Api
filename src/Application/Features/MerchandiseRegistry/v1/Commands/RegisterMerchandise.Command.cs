using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using MediatR;

namespace ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Commands;

public class RegisterMerchandiseCommand : BaseRequest, IRequest<Guid>
{
    public string MerchandiseName { get; set; } = null!;
    public string? Description { get; set; }
    public Guid CategoryId { get; set; }
}