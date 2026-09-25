using MediatR;
using ERP.Core.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Commands;

public class DeleteReceptionEntranceCommand : BaseRequest, IRequest<bool>
{
    public Guid ReceptionId { get; set; }
}