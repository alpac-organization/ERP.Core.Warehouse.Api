namespace ERP.Core.Warehouse.Api.Application.Commons.Options
{
    public class QrConfig
    {
        public Dictionary<string, ClientQrConfig> Clients { get; set; } = new();
    }

    public class ClientQrConfig
    {
        public string? BaseRedirectUrl { get; set; }
    }
}