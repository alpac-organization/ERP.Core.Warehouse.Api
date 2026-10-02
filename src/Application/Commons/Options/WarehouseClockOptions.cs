namespace ERP.Core.Warehouse.Api.Application.Commons.Options
{
    public class WarehouseClockOptions
    {
        public string BaseUrl { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; } = 5;
        public string Command { get; set; } = string.Empty;
        public string ClearCommand { get; set; } = string.Empty;
    }
}