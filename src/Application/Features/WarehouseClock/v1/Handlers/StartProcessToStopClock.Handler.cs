using ERP.Core.Warehouse.Api.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseClock.v1.Commands;
using MediatR;

namespace ERP.Core.Warehouse.Api.Application.Features.WarehouseClock.v1.Handlers
{
    public class StartProcessToStopClockHandler(IWarehouseClockServices _clockServices) : IRequestHandler<StartProcessToStopClockCommand>
    {
        public async Task Handle(StartProcessToStopClockCommand request, CancellationToken cancellationToken)
        {
            await _clockServices.TurnOffAsync(cancellationToken);
        }
    }
}