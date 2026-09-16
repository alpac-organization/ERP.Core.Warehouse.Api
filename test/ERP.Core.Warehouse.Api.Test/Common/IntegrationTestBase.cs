using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;

using NUnit.Framework;
using Microsoft.Extensions.DependencyInjection;

using ERP.Core.Warehouse.Api.Test.Common.Utils;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

namespace ERP.Core.Warehouse.Api.Test.Common
{
    [TestFixture]
    public abstract class IntegrationTestBase
    {
        private IServiceScope _scope = null!;

        protected HttpClient _client = null!;
        protected IUnitOfWork _unitOfWork = null!;
        protected static CustomWebApplicationFactory Factory => PostgreSqlContainerFixture.Factory;

        [SetUp]
        public async Task SetUp()
        {
            _client = PostgreSqlContainerFixture.Factory.CreateClient();

            //Limpiamos la base de datos para el uso de ella
            await Factory.ResetDatabase();
            await Factory.SeedDatabase();

            //Definir los servicios
            _scope = Factory.Services.CreateScope();
            _unitOfWork = ServiceProviderServiceExtensions.GetRequiredService<IUnitOfWork>(_scope.ServiceProvider);
        }

        [TearDown]
        public void TearDown()
        {
            _scope.Dispose();
            _client.Dispose();
        }

        protected async Task<HttpResponseMessage> SendRequestAsync(HttpMethod method, string pathUrl, Guid userId, object? body)
        {
            var request = new HttpRequestMessage(method, pathUrl);
            
            request.Headers.Add("X-Api-Key", EnvironmentManager.ApiKey);

            //Generar un token valido, para poder pasar la validación correctamente, de un usuario que exista, de lo contrario nunca pasara de access!
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer", 
                AuthManager.GenerateJwtToken(Factory.JwtKey, userId)
            );

            if(body != null && (method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Patch))
            {
                var jsonBody = JsonSerializer.Serialize(body);
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            }

            return await _client.SendAsync(request);
        }
    }
}
