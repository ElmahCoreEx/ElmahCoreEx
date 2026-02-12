using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ElmahCore.Mvc;
using ElmahCore.Mvc.Exceptions;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

// ReSharper disable once CheckNamespace
namespace ElmahCore;

public static class ElmahExtensions
{
    internal static ErrorLogMiddleware LogMiddleware;

    private static void GuardForNullMiddleware()
    {
        if (LogMiddleware == null)
            throw new MiddlewareNotInitializedException("Elmah Middleware not initialized");
    }

    extension(HttpContext ctx)
    {
        [Obsolete("Prefer RaiseError, will be removed in next major version")]
        public Task RiseError(Exception ex, Func<HttpContext, Error, Task> onError)
        {
            return ctx.RaiseError(ex, onError);
        }

        public Task RaiseError(Exception ex, Func<HttpContext, Error, Task> onError)
        {
            GuardForNullMiddleware();
            return LogMiddleware.LogException(ex, ctx, onError);
        }

        [Obsolete("Prefer RaiseError, will be removed in next major version")]
        public Task RiseError(Exception ex)
        {
            return ctx.RaiseError(ex);
        }

        public Task RaiseError(Exception ex)
        {
            GuardForNullMiddleware();
            return LogMiddleware.LogException(ex, ctx, (_, _) => Task.CompletedTask);
        }

        public Task RaiseError(Exception ex, int statusCode)
        {
            GuardForNullMiddleware();
            return LogMiddleware.LogException(ex, ctx, (_, _) => Task.CompletedTask, statusCode: statusCode);
        }

        public Task RaiseError(Exception ex, int statusCode,
            Func<HttpContext, Error, Task> onError)
        {
            GuardForNullMiddleware();
            return LogMiddleware.LogException(ex, ctx, onError, statusCode: statusCode);
        }
    }

    // ReSharper disable once MemberCanBePrivate.Global

    [Obsolete("Prefer RaiseError")]
    public static void RiseError(Exception ex)
    {
        RaiseError(ex);
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public static void RaiseError(Exception ex, Func<HttpContext, Error, Task> onError)
    {
        LogMiddleware?.LogException(ex, InternalHttpContext.Current ?? new DefaultHttpContext(),
            onError);
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public static void RaiseError(Exception ex)
    {
        RaiseError(ex, (_, _) => Task.CompletedTask);
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public static void RaiseError(Exception ex, int statusCode)
    {
        RaiseError(ex, statusCode, (_, _) => Task.CompletedTask);
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public static void RaiseError(Exception ex, int statusCode, Func<HttpContext, Error, Task> onError)
    {
        LogMiddleware?.LogException(ex, InternalHttpContext.Current ?? new DefaultHttpContext(),
            onError, statusCode: statusCode);
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