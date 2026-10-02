using MediatR;
using Microsoft.Extensions.Options;
using ERP.Core.Warehouse.Api.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Commons.Options;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseClock.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.WarehouseClock.v1.Handlers
{
    public class StartProcessToStopClockHandler(IWarehouseClockServices _clockServices, IOptions<WarehouseClockOptions> _options) : IRequestHandler<StartProcessToStopClockCommand>
    {
        public async Task Handle(StartProcessToStopClockCommand request, CancellationToken cancellationToken)
        {
            var options = _options.Value;

            foreach (var clock in options.Clocks)
            {
                await _clockServices.ClearEventsAsync(clock, options.TimeoutSeconds);
            }
        }
    }
}   