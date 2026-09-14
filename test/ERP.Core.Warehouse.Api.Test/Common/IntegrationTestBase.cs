using NUnit.Framework;

namespace ERP.Core.Warehouse.Api.Test.Common
{
    [TestFixture]
    public abstract class IntegrationTestBase
    {
        protected HttpClient _client = null!;


        protected async Task<HttpResponseMessage> SendRequestAsync<T>(HttpMethod method, string pathUrl, T? body)
        {
            var request = new HttpRequestMessage(method, pathUrl);


            return await _client.SendAsync(request);
        }
    }
}