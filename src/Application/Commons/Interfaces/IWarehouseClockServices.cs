using ERP.Core.Warehouse.Api.Application.Commons.Options;

namespace ERP.Core.Warehouse.Api.Application.Commons.Interfaces
{
    public interface IWarehouseClockServices
    {
        Task ClearEventsAsync(ClockConfig clock, int timeoutSeconds);
        Task SetEventsAsync(ClockConfig clock, int timeoutSeconds);
    }
}