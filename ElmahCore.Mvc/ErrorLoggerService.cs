using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ElmahCore.Mvc;

/// <summary>
/// Default implementation of <see cref="IErrorLogger"/> that provides
/// dependency injection-based access to ELMAH error logging.
/// </summary>
internal sealed class ErrorLoggerService : IErrorLogger
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ErrorLog _errorLog;

    // Holds reference to the middleware for full logging support (filters, notifiers, etc.)
    private static ErrorLogMiddleware _middleware;

    public ErrorLoggerService(IHttpContextAccessor httpContextAccessor, ErrorLog errorLog)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _errorLog = errorLog ?? throw new ArgumentNullException(nameof(errorLog));
    }

    /// <summary>
    /// Called by the middleware to register itself for full logging support.
    /// </summary>
    internal static void SetMiddleware(ErrorLogMiddleware middleware)
    {
        _middleware = middleware;
    }

    /// <inheritdoc />
    public Task<string> LogAsync(Exception exception)
    {
        return LogAsync(exception, statusCode: null);
    }

    /// <inheritdoc />
    public Task<string> LogAsync(Exception exception, int statusCode)
    {
        return LogAsync(exception, (int?)statusCode);
    }

    /// <inheritdoc />
    public async Task<string> LogAsync(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        error.ApplicationName = _errorLog.ApplicationName;
        return await _errorLog.LogAsync(error);
    }

    private async Task<string> LogAsync(Exception exception, int? statusCode)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var context = _httpContextAccessor.HttpContext ?? new DefaultHttpContext();

        // If middleware is available, use it for full support (filters, notifiers, events)
        if (_middleware != null)
        {
            return await _middleware.LogException(
                exception,
                context,
                (_, _) => Task.CompletedTask,
                body: null,
                statusCode: statusCode);
        }

        // Fallback: log directly to ErrorLog without filters/notifiers
        var error = new Error(exception, context);
        if (statusCode.HasValue)
            error.StatusCode = statusCode.Value;

        error.ApplicationName = _errorLog.ApplicationName;
        return await _errorLog.LogAsync(error);
    }
}
