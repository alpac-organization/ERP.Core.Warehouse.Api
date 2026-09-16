
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
        public async Task RegistePurchaseRequestWhenIsSuccess(string companyAlias)
        {
            var companies = await _unitOfWork.Companies.Entities
                .Where(company => company.IsActive)
                .Where(company => company.Alias == companyAlias)
                .ToListAsync(default);

            

            //Generar su propio bearer token aqui

        }
    }
}
