
using NUnit.Framework;
using ERP.Core.Warehouse.Api.Test.Common;
using System.Net;

namespace ERP.Core.Warehouse.Api.Test.Controllers
{
    [TestFixture]
    public class PurchaseRequestControllerTest : IntegrationTestBase
    {
 
        [Test]
        public async Task RegistePurchaseRequestWhenIsSuccess()
        {

            var response = await SendRequestAsync(HttpMethod.Get, $"/api/v1/companies/aaaaaa/modules/cccccc/purchase-requests", Guid.Empty, null);

            // 5. Verificamos el resultado
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }
    }
}
