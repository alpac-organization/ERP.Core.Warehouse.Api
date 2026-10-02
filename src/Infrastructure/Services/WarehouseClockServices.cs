using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ERP.Core.Warehouse.Api.Application.Commons.Options;
using ERP.Core.Warehouse.Api.Application.Commons.Interfaces;

namespace ERP.Core.Warehouse.Api.Infrastructure.Services
{
    public class WarehouseClockServices(IHttpClientFactory httpClientFactory, ILogger<WarehouseClockServices> logger) : IWarehouseClockServices
    {
        public async Task ClearEventsAsync(ClockConfig clock, int timeoutSeconds)
        {
            try
            {
                var client = httpClientFactory.CreateClient();
                client.BaseAddress = new Uri(clock.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
                
                await PostEventsAsync(client, clock.ClearCommand, clock.BaseUrl, "ClearEvents");
                logger.LogInformation("Warehouse Clock events cleared for {BaseUrl}", clock.BaseUrl);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error clearing events from Warehouse Clock at {BaseUrl}", clock.BaseUrl);
            }
        }

        public async Task SetEventsAsync(ClockConfig clock, int timeoutSeconds)
        {
            try
            {
                var client = httpClientFactory.CreateClient();
                client.BaseAddress = new Uri(clock.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
                
                await PostEventsAsync(client, clock.Command, clock.BaseUrl, "SetEvents");
                logger.LogInformation("Warehouse Clock events set for {BaseUrl}", clock.BaseUrl);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error setting events to Warehouse Clock at {BaseUrl}", clock.BaseUrl);
            }
        }

        private async Task PostEventsAsync(HttpClient client, string command, string baseUrl, string operation)
        {
            logger.LogInformation("{Operation} - Sending to {BaseUrl}/set-events: {Command}", operation, baseUrl, command);
            
            using var content = new StringContent(command, Encoding.UTF8, "application/json");
            using var response = await client.PostAsync("/set-events", content);
            
            var responseBody = await response.Content.ReadAsStringAsync();
            logger.LogInformation("{Operation} - Response from {BaseUrl}: Status={StatusCode}, Body={Body}", 
                operation, baseUrl, response.StatusCode, responseBody);
            
            response.EnsureSuccessStatusCode();
        }
    }
}