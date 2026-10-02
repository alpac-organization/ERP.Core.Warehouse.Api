using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ERP.Core.Warehouse.Api.Application.Commons.Options;
using ERP.Core.Warehouse.Api.Application.Commons.Interfaces;

namespace ERP.Core.Warehouse.Api.Infrastructure.Services
{
    public class WarehouseClockServices(HttpClient httpClient, IOptions<WarehouseClockOptions> options, ILogger<WarehouseClockServices> logger) : IWarehouseClockServices
    {
        private const string TurnOnPath = "/api/power?state=on";
        private const string TurnOffPath = "/api/power?state=off";
        private const string CounterPath = "/api/counter";

        public async Task TurnOnAsync(CancellationToken ct = default)
        {
            try
            {
                logger.LogInformation("Encendiendo reloj WS-604S en {BaseUrl}", options.Value.BaseUrl);
                using var response = await httpClient.PostAsync(TurnOnPath, null, ct);
                response.EnsureSuccessStatusCode();
                logger.LogInformation("Reloj WS-604S encendido correctamente");
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "Error de red al encender el reloj WS-604S");
            }
            catch (TaskCanceledException ex)
            {
                logger.LogError(ex, "Tiempo de espera agotado al encender el reloj WS-604S");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error inesperado al encender el reloj WS-604S");
            }
        }

        public async Task TurnOffAsync(CancellationToken ct = default)
        {
            try
            {
                logger.LogInformation("Apagando reloj WS-604S en {BaseUrl}", options.Value.BaseUrl);
                using var response = await httpClient.PostAsync(TurnOffPath, null, ct);
                response.EnsureSuccessStatusCode();
                logger.LogInformation("Reloj WS-604S apagado correctamente");
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "Error de red al apagar el reloj WS-604S");
            }
            catch (TaskCanceledException ex)
            {
                logger.LogError(ex, "Tiempo de espera agotado al apagar el reloj WS-604S");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error inesperado al apagar el reloj WS-604S");
            }
        }

        public async Task SetCounterAsync(int minutes, CancellationToken ct = default)
        {
            try
            {
                var url = $"{CounterPath}?minutes={minutes}";
                logger.LogInformation("Configurando contador del reloj WS-604S a {Minutes} minutos", minutes);
                using var response = await httpClient.PostAsync(url, null, ct);
                response.EnsureSuccessStatusCode();
                logger.LogInformation("Contador del reloj WS-604S configurado a {Minutes} minutos", minutes);
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "Error de red al configurar contador del reloj WS-604S");
            }
            catch (TaskCanceledException ex)
            {
                logger.LogError(ex, "Tiempo de espera agotado al configurar contador del reloj WS-604S");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error inesperado al configurar contador del reloj WS-604S");
            }
        }

        public async Task ClearEventsAsync()
        {
            try
            {
                await PostEventsAsync(options.Value.ClearCommand);
                logger.LogInformation("Warehouse Clock events cleared.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error clearing events from Warehouse Clock.");
                throw;
            }
        }

        public async Task SetEventsAsync()
        {
            try
            {
                await PostEventsAsync(options.Value.Command);
                logger.LogInformation("Warehouse Clock events set.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error setting events to Warehouse Clock.");
                throw;
            }
        }

        private async Task PostEventsAsync(string command)
        {
            var body = JsonSerializer.Serialize(command);

            using var content = new StringContent(body, Encoding.UTF8, "application/json");
            using var response = await httpClient.PostAsync($"{options.Value.BaseUrl}/set-events", content);
            
            response.EnsureSuccessStatusCode();
        }
    }
}