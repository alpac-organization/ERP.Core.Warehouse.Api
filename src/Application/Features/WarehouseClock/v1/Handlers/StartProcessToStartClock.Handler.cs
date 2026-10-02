using ERP.Core.Warehouse.Api.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseClock.v1.Commands;
using MediatR;

namespace ERP.Core.Warehouse.Api.Application.Features.WarehouseClock.v1.Handlers
{
    public class StartProcessToStartClockHandler(IWarehouseClockServices _clockServices) : IRequestHandler<StartProcessToStartClockCommand>
    {
        public async Task Handle(StartProcessToStartClockCommand request, CancellationToken cancellationToken)
        {
            await _clockServices.TurnOnAsync(cancellationToken);
        }
    }
}