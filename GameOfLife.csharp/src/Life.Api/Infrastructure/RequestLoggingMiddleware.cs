using System.Diagnostics;

namespace Life.Api.Infrastructure;

/// <summary>
/// Middleware for logging HTTP requests and responses.
/// This middleware logs the request method, path, query string, response status code,
/// duration of the request, and the client's IP address.
/// It is used to monitor and debug the application's HTTP traffic.
/// </summary>
/// <remarks>
/// Uses <see cref="RequestLoggingMiddleware"/> to log HTTP requests.
/// This class is responsible for logging details of each HTTP request processed by the application.
/// It captures the request method, path, query string, response status code, duration of the request,
/// and the client's IP address. This information is useful for monitoring and debugging purposes.
/// </remarks>
public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        => (_next, _logger) = (next, logger);

    /// <summary>
    /// Logs the details of an HTTP request.
    /// </summary>
    /// <param name="ctx">The HTTP context containing the request and response.</param>
    /// <remarks>
    /// This method is called for each HTTP request processed by the middleware.
    /// It logs the request method, path, query string, response status code,
    /// duration of the request, and the client's IP address.
    /// The duration is measured using a stopwatch to capture the time taken to process the request.
    /// </remarks>
    /// <example>
    /// <code>
    /// app.UseMiddleware<RequestLoggingMiddleware>();
    /// </code>
    /// </example>
    public async Task Invoke(HttpContext ctx)
    {
        var sw = Stopwatch.StartNew();
        await _next(ctx);
        sw.Stop();

        _logger.LogInformation(
            "Request {Method} {Path} {Query} {Status} {DurationMs} {Client}",
            ctx.Request.Method,
            ctx.Request.Path.Value,
            ctx.Request.QueryString.Value,
            ctx.Response.StatusCode,
            sw.ElapsedMilliseconds,
            ctx.Connection.RemoteIpAddress?.ToString());
    }
}
