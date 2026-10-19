using MediatR;
using ERP.Core.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryEntries.v1.Commands
{
    public class RegisterInventoryEntryCommand : BaseRequest, IRequest<Unit>
    {
        
    }
}