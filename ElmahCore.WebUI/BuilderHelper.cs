using System;
using System.Diagnostics;
using ElmahCore.WebUI.Logger;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ElmahCore.WebUI
{
    public static class BuilderHelper
    {
        [UsedImplicitly]
        public static IApplicationBuilder UseElmahExceptionPage(this IApplicationBuilder app)
        {
            ErrorLogWebUIMiddleware.ShowDebugPage = true;
            return app;
        }

        [UsedImplicitly]
        public static IApplicationBuilder UseElmah(this IApplicationBuilder app)
        {
            app.UseStaticHttpContext();

            var elmahOptions = app.ApplicationServices.GetService<IOptions<ElmahOptions>>();
            // if (elmahOptions == null || elmahOptions.Value.EnableDiagnosticObserver)
            // {
            //     DiagnosticListener.AllListeners.Subscribe(new ElmahDiagnosticObserver(app.ApplicationServices));
            // }

            app.UseMiddleware<ErrorLogWebUIMiddleware>();
            return app;
        }

        public static IServiceCollection AddElmah(this IServiceCollection services)
        {
            return AddElmah<MemoryErrorLog>(services);
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public static IServiceCollection AddElmah<T>(this IServiceCollection services) where T : ErrorLog
        {
            services.AddHttpContextAccessor();
            services.AddSingleton<ILoggerProvider>(provider =>
                new ElmahLoggerProvider(provider.GetService<IHttpContextAccessor>()));

            return services.AddSingleton<ErrorLog, T>();
        }

        public static IServiceCollection SetElmahLogLevel(this IServiceCollection services, LogLevel level)
        {
            services.AddLogging(builder => { builder.AddFilter<ElmahLoggerProvider>(l => l >= level); });
            return services;
        }

        [UsedImplicitly]
        public static IServiceCollection AddElmah(this IServiceCollection services, Action<ElmahOptions> setupAction)
        {
            return AddElmah<MemoryErrorLog>(services, setupAction);
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public static IServiceCollection AddElmah<T>(this IServiceCollection services, Action<ElmahOptions> setupAction)
            where T : ErrorLog
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            if (setupAction == null) throw new ArgumentNullException(nameof(setupAction));

            var builder = services.AddElmah<T>();
            builder.Configure(setupAction);
            return builder;
        }
    }
}