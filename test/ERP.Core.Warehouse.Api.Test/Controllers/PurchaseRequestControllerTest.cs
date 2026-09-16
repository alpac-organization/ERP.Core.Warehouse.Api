
using System.Net;
using NUnit.Framework;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Warehouse.Api.Test.Common;

namespace ERP.Core.Warehouse.Api.Test.Controllers
{
    [TestFixture]
    public class PurchaseRequestControllerTest : IntegrationTestBase
    {
        private static string PurchaseRequestBaseUrl(Guid companyId, string moduleCode) => $"/api/v1/companies/${companyId}/modules/${moduleCode}/purchase-requests";

        [Test]
        [TestCase("ALPAC")]
        [TestCase("AMINSA")]
        [Order(1)]
        public async Task RegistePurchaseRequestWhenIsSuccess(string companyAlias)
        {
            //Creamos nuestro usuario
            var userId = await CreateUser();
            

            //Generar su propio bearer token aqui

        }
    }
}
