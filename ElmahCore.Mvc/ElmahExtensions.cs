using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ElmahCore.Mvc;
using ElmahCore.Mvc.Exceptions;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

// ReSharper disable once CheckNamespace
namespace ElmahCore;

/// <summary>
///     Extension methods for logging errors to ELMAH.
/// </summary>
/// <remarks>
///     For new code, prefer injecting <see cref="IErrorLogger"/> via dependency injection
///     instead of using the static methods on this class.
/// </remarks>
public static class ElmahExtensions
{
    /// <summary>
    ///     Internal reference to the middleware. Prefer using <see cref="IErrorLogger"/> via DI.
    /// </summary>
    [Obsolete("Use IErrorLogger via dependency injection instead. This field will be removed in a future version.")]
    internal static ErrorLogMiddleware LogMiddleware;

    private static void GuardForNullMiddleware()
    {
#pragma warning disable CS0618 // Type or member is obsolete
        if (LogMiddleware == null)
            throw new MiddlewareNotInitializedException("Elmah Middleware not initialized");
#pragma warning restore CS0618
    }

    extension(HttpContext ctx)
    {
        [Obsolete("Prefer RaiseError, will be removed in next major version")]
        public Task RiseError(Exception ex, Func<HttpContext, Error, Task> onError)
        {
            return ctx.RaiseError(ex, onError);
        }

        /// <summary>
        ///     Logs an exception with a custom error handler.
        /// </summary>
        public Task RaiseError(Exception ex, Func<HttpContext, Error, Task> onError)
        {
            GuardForNullMiddleware();
#pragma warning disable CS0618 // Type or member is obsolete
            return LogMiddleware.LogException(ex, ctx, onError);
#pragma warning restore CS0618
        }

        [Obsolete("Prefer RaiseError, will be removed in next major version")]
        public Task RiseError(Exception ex)
        {
            return ctx.RaiseError(ex);
        }

        /// <summary>
        ///     Logs an exception.
        /// </summary>
        public Task RaiseError(Exception ex)
        {
            GuardForNullMiddleware();
#pragma warning disable CS0618 // Type or member is obsolete
            return LogMiddleware.LogException(ex, ctx, (_, _) => Task.CompletedTask);
#pragma warning restore CS0618
        }

        /// <summary>
        ///     Logs an exception with a specific HTTP status code.
        /// </summary>
        public Task RaiseError(Exception ex, int statusCode)
        {
            GuardForNullMiddleware();
#pragma warning disable CS0618 // Type or member is obsolete
            return LogMiddleware.LogException(ex, ctx, (_, _) => Task.CompletedTask, statusCode: statusCode);
#pragma warning restore CS0618
        }

        /// <summary>
        ///     Logs an exception with a specific HTTP status code and custom error handler.
        /// </summary>
        public Task RaiseError(Exception ex, int statusCode,
            Func<HttpContext, Error, Task> onError)
        {
            GuardForNullMiddleware();
#pragma warning disable CS0618 // Type or member is obsolete
            return LogMiddleware.LogException(ex, ctx, onError, statusCode: statusCode);
#pragma warning restore CS0618
        }
    }

    // ReSharper disable once MemberCanBePrivate.Global

    [Obsolete("Use IErrorLogger via dependency injection instead.")]
    public static void RiseError(Exception ex)
    {
        RaiseError(ex);
    }

    /// <summary>
    ///     Logs an exception with a custom error handler.
    /// </summary>
    /// <remarks>
    ///     This static method is provided for backward compatibility.
    ///     Prefer injecting <see cref="IErrorLogger"/> via dependency injection.
    /// </remarks>
    [Obsolete("Use IErrorLogger via dependency injection instead. Static methods will be removed in a future version.")]
    // ReSharper disable once MemberCanBePrivate.Global
    public static void RaiseError(Exception ex, Func<HttpContext, Error, Task> onError)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        LogMiddleware?.LogException(ex, InternalHttpContext.Current ?? new DefaultHttpContext(),
            onError);
#pragma warning restore CS0618
    }

    /// <summary>
    ///     Logs an exception.
    /// </summary>
    /// <remarks>
    ///     This static method is provided for backward compatibility.
    ///     Prefer injecting <see cref="IErrorLogger"/> via dependency injection.
    /// </remarks>
    [Obsolete("Use IErrorLogger via dependency injection instead. Static methods will be removed in a future version.")]
    // ReSharper disable once MemberCanBePrivate.Global
    public static void RaiseError(Exception ex)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        RaiseError(ex, (_, _) => Task.CompletedTask);
#pragma warning restore CS0618
    }

    /// <summary>
    ///     Logs an exception with a specific HTTP status code.
    /// </summary>
    /// <remarks>
    ///     This static method is provided for backward compatibility.
    ///     Prefer injecting <see cref="IErrorLogger"/> via dependency injection.
    /// </remarks>
    [Obsolete("Use IErrorLogger via dependency injection instead. Static methods will be removed in a future version.")]
    // ReSharper disable once MemberCanBePrivate.Global
    public static void RaiseError(Exception ex, int statusCode)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        RaiseError(ex, statusCode, (_, _) => Task.CompletedTask);
#pragma warning restore CS0618
    }

    /// <summary>
    ///     Logs an exception with a specific HTTP status code and custom error handler.
    /// </summary>
    /// <remarks>
    ///     This static method is provided for backward compatibility.
    ///     Prefer injecting <see cref="IErrorLogger"/> via dependency injection.
    /// </remarks>
    [Obsolete("Use IErrorLogger via dependency injection instead. Static methods will be removed in a future version.")]
    // ReSharper disable once MemberCanBePrivate.Global
    public static void RaiseError(Exception ex, int statusCode, Func<HttpContext, Error, Task> onError)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        LogMiddleware?.LogException(ex, InternalHttpContext.Current ?? new DefaultHttpContext(),
            onError, statusCode: statusCode);
#pragma warning restore CS0618
    }

    [UsedImplicitly]
    public static void LogParams(this object source,
        (string name, object value) param1 = default,
        (string name, object value) param2 = default,
        (string name, object value) param3 = default,
        (string name, object value) param4 = default,
        (string name, object value) param5 = default,
        (string name, object value) param6 = default,
        (string name, object value) param7 = default,
        (string name, object value) param8 = default,
        (string name, object value) param9 = default,
        (string name, object value) param10 = default,
        [CallerMemberName] string memberName = null,
        [CallerFilePath] string file = null,
        [CallerLineNumber] int line = 0)
    {
        try
        {
            var feature = InternalHttpContext.Current.Features.Get<ElmahLogFeature>();
            if (feature == null) return;

            var list = new[] { param1, param2, param3, param4, param5, param6, param7, param8, param9, param10 };

            var typeName = source.GetType().ToString();
            feature.LogParameters(list, typeName, memberName, file, line);
        }
        catch
        {
            // ignored
        }
    }
}