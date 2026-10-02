namespace ERP.Core.Warehouse.Api.Application.Commons.Interfaces
{
    public interface IWarehouseClockServices
    {
        Task TurnOnAsync(CancellationToken ct = default);
        Task TurnOffAsync(CancellationToken ct = default);
        Task SetCounterAsync(int minutes, CancellationToken ct = default);
        
        /// <summary>Borra todos los eventos del reloj.</summary>
        Task ClearEventsAsync();

        /// <summary>Programa los eventos de countdown según los bloques de appsettings.</summary>
        Task SetEventsAsync();
    }
}