using ERP.Core.Warehouse.Api.Test.Common;
using NUnit.Framework;

namespace ERP.Core.Warehouse.Api.Test.Controllers.Warehouse;

[TestFixture]
public class RegisterLotsControllerTest : IntegrationTestUtilsBase
{
    private static string RegisterLotsBaseUrl
        (Guid companyId, string moduleCode, Guid warehouseId, Guid sectionId)
            => $"api/v1/companies/{companyId}/modules/{moduleCode}/warehouses/{warehouseId}/sections/{sectionId}/lots";

    // [Test]
    // [TestCase("ALPAC")]

}