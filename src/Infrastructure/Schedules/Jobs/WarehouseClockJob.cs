using Quartz;
using MediatR;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseClock.v1.Commands;

namespace ERP.Core.Warehouse.Api.Infrastructure.Schedules.Jobs
{
    [DisallowConcurrentExecution]
    public class WarehouseStartProcessToStartClockJob(ISender _mediator): IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            await _mediator.Send(new StartProcessToStartClockCommand());
        }
    }

    [DisallowConcurrentExecution]
    public class WarehouseStartProcessToStopClockJob(ISender _mediator): IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            await _mediator.Send(new StartProcessToStopClockCommand());
        }
    }
}