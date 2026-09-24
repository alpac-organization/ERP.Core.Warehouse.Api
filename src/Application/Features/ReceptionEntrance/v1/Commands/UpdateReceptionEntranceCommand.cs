using MediatR;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Commands
{
    public class UpdateReceptionEntranceCommand : BaseRequest, IRequest<bool>
    {
        
    }
}