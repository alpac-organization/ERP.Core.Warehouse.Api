    using System.Text;
    using System.Text.Json;

    using NUnit.Framework;
    using System.Net.Http.Headers;

    using ERP.Core.Warehouse.Api.Test.Common.Utils;

    namespace ERP.Core.Warehouse.Api.Test.Common
    {
        [TestFixture]
        public abstract class IntegrationTestBase
        {
            protected HttpClient _client = null!;
            protected static CustomWebApplicationFactory Factory => PostgreSqlContainerFixture.Factory;

            [SetUp]
            public void SetUp()
            {
                _client = PostgreSqlContainerFixture.Factory.CreateClient();
            }

            protected async Task<HttpResponseMessage> SendRequestAsync(HttpMethod method, string pathUrl, Guid userId, object? body)
            {
                var request = new HttpRequestMessage(method, pathUrl);

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
