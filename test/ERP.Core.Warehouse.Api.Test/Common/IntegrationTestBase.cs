using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;

using NUnit.Framework;
using Microsoft.Extensions.DependencyInjection;

using ERP.Core.Warehouse.Api.Test.Common.Utils;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Auth;

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

        protected async Task<HttpResponseMessage> SendRequestAsync(HttpMethod method, string pathUrl, string BearerToken, object? body = null)
        {
            var request = new HttpRequestMessage(method, pathUrl);
            
            request.Headers.Add("X-Api-Key", EnvironmentManager.ApiKey);

            //Generar un token valido, para poder pasar la validación correctamente, de un usuario que exista, de lo contrario nunca pasara de access!
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer", BearerToken
            );

            if(body != null && (method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Patch))
            {
                var jsonBody = JsonSerializer.Serialize(body);
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            }

            return await _client.SendAsync(request);
        }

        #region  Method Utils

        public async Task<Guid?> CreateUser(Guid? areaId = null)
        {
            Guid workAreaId;

            if (areaId.HasValue) workAreaId = areaId.Value;
            else
            {
                var area = await _unitOfWork.WorkAreas.Entities
                    .Where(workArea => workArea.IsActive)
                    .Where(workArea => workArea.WorkAreaCode == 10)
                    .FirstOrDefaultAsync(default);

                if (area is null) return null;

                workAreaId = area.Id;
            }

            var newUserId = Guid.NewGuid();

            await _unitOfWork.Users.CreateNewUser(new()
            {
                Id = newUserId,
                UserType = UserType.StandardUser,
                UserName = "carlos.mendoza",
                PasswordHash = "$hashpassoword",
                Fullname = "Carlos Alberto Mendoza Gutiérrez",
                Email = "testing@domain.com",
                AreaId = workAreaId,
                IdentificationNumber = "0010101011052A",
            });

            await _unitOfWork.SaveChangesAsync();

            return newUserId;
        }

        #endregion
    }
}
