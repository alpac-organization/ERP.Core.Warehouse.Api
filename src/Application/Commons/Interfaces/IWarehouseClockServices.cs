namespace ERP.Core.Warehouse.Api.Application.Commons.Interfaces
{
    public interface IWarehouseClockServices
    {
    /// <summary>Borra todos los eventos del reloj.</summary>
        Task ClearEventsAsync();

        /// <summary>Programa los eventos de countdown según los bloques de appsettings.</summary>
        Task SetEventsAsync();
    }
}