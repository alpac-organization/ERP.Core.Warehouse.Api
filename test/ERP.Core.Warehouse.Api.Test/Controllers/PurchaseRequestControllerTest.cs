
using System.Net;
using NUnit.Framework;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Warehouse.Api.Test.Common;
using ERP.Core.Warehouse.Api.Test.Common.Utils;
using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Test.Controllers
{
    [TestFixture]
    public class PurchaseRequestControllerTest : IntegrationTestUtilsBase
    {
        private static string PurchaseRequestBaseUrl(Guid companyId, string moduleCode) => $"/api/v1/companies/{companyId}/modules/{moduleCode}/purchase-requests";

        [Test]
        [TestCase("ALPAC")]
        public async Task RegistePurchaseRequestWhenIsSuccess(string companyAlias)
        {
            //Creamos nuestro usuario
            Guid userId = await CreateUser("Carlos Alberto Mendoza Gutiérrez");

            var bearerToken = AuthManager.GenerateJwtToken(EnvironmentManager.JwtKey, userId);


            var company = await _unitOfWork.Companies.Entities
                .Where(company => company.IsActive) 
                .Where(company => company.Alias == companyAlias) 
                .FirstOrDefaultAsync(default);

            //Your payload here!
            object payload = new()
            {
                //Your body here    
            };

            //Necesitamos crear un producto para la solicitudes de compras, you module here
            await SendRequestAsync(HttpMethod.Post, PurchaseRequestBaseUrl(company!.Id, "MODULECODE"), bearerToken, payload);

            //Assert here.

        }

    }
}
