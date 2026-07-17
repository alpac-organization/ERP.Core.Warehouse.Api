namespace ERP.Core.Warehouse.Api.Application.Commons.Utils;

public static class NicaraguaClock
{
    private static readonly TimeSpan Offset = TimeSpan.FromHours(-6);
    public static DateTime Now => DateTime.UtcNow.Add(Offset);
    public static DateOnly Today => DateOnly.FromDateTime(Now);
    public static TimeOnly TimeNow => TimeOnly.FromDateTime(Now);
}