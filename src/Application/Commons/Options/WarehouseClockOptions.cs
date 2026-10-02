namespace ERP.Core.Warehouse.Api.Application.Commons.Options
{
    public class WarehouseClockOptions
    {
        public List<ClockConfig> Clocks { get; set; } = new();
        public int TimeoutSeconds { get; set; } = 5;
    }

    public class ClockConfig
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
        public string ClearCommand { get; set; } = string.Empty;
    }
}