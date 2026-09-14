using NUnit.Framework;

namespace ERP.Core.Warehouse.Api.Test.Common
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
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            await Factory.DisposePostgreSqlContainer();
        }
    }
}