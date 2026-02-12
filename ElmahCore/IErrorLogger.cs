using System;
using System.Threading.Tasks;

namespace ElmahCore;

/// <summary>
/// Interface for logging errors to ELMAH.
/// Use this interface for dependency injection instead of static methods.
/// </summary>
public interface IErrorLogger
{
    /// <summary>
    /// Logs an exception asynchronously.
    /// </summary>
    /// <param name="exception">The exception to log.</param>
    /// <returns>The error ID, or null if the error was filtered.</returns>
    Task<string> LogAsync(Exception exception);

    /// <summary>
    ///     Logs an exception with a specific HTTP status code asynchronously.
    /// </summary>
    /// <param name="exception">The exception to log.</param>
    /// <param name="statusCode">The HTTP status code to associate with the error.</param>
    /// <returns>The error ID, or null if the error was filtered.</returns>
    Task<string> LogAsync(Exception exception, int statusCode);

    /// <summary>
    ///     Logs an error object directly.
    /// </summary>
    /// <param name="error">The error to log.</param>
    /// <returns>The error ID.</returns>
    Task<string> LogAsync(Error error);
}
