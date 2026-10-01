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