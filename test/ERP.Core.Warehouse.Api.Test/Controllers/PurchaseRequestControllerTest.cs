
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
        [TestCase("")]
        [TestCase("")]
        public async Task RegistePurchaseRequestWhenIsSuccess(string companyAlias)
        {
            var companies = _unitOfWork.Companies.Entities
                .Where(company => company.IsActive)
                .ToListAsync(default);


            var response = await SendRequestAsync(HttpMethod.Get, $"{PurchaseRequestBaseUrl(Guid.Empty, "NOMINA")}", Guid.Empty, null);

            // 5. Verificamos el resultado
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }
    }
}
