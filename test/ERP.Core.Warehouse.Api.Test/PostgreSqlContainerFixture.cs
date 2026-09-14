using NUnit.Framework;

namespace ERP.Core.Warehouse.Api.Test
{
    [SetUpFixture]
    public class PostgreSqlContainerFixture
    {
        public static CustomWebApplicationFactory Factory { get; private set; } = null!;

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            Factory = new();
            await Factory.InitializePostgreSqlContainer();

            //Devolvemos la causa de por que no corren los test.
            if (!Factory.IsDockerAvailable)
            {
                Assert.Ignore("Docker no está disponible. Se omiten las pruebas de integración.");
            }
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            await Factory.DisposePostgreSqlContainer();
        }
    }
}