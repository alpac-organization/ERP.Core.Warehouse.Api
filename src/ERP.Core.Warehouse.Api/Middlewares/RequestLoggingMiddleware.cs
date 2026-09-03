using System.Diagnostics;

namespace ERP.Core.Warehouse.Api.Middlewares
{
    public class RequestLoggingMiddleware(RequestDelegate _next, ILogger<RequestLoggingMiddleware> _logger)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Excepción no controlada para {Method} {Path}{Query}",
                    context.Request.Method, context.Request.Path, context.Request.QueryString);
                throw;
            }
            finally
            {
                stopwatch.Stop();

                var statusCode = context.Response.StatusCode;

                if (statusCode >= 500)
                {
                    _logger.LogError("HTTP {Status} {Method} {Path}{Query} - {Elapsed}ms [TraceId={TraceId}]",
                        statusCode, context.Request.Method, context.Request.Path, context.Request.QueryString,
                        stopwatch.ElapsedMilliseconds, context.TraceIdentifier);
                }
                else if (statusCode >= 400)
                {
                    _logger.LogWarning("HTTP {Status} {Method} {Path}{Query} - {Elapsed}ms [TraceId={TraceId}]",
                        statusCode, context.Request.Method, context.Request.Path, context.Request.QueryString,
                        stopwatch.ElapsedMilliseconds, context.TraceIdentifier);
                }
                else
                {
                    _logger.LogInformation("HTTP {Status} {Method} {Path}{Query} - {Elapsed}ms [TraceId={TraceId}]",
                        statusCode, context.Request.Method, context.Request.Path, context.Request.QueryString,
                        stopwatch.ElapsedMilliseconds, context.TraceIdentifier);
                }
            }
        }
    }
}
