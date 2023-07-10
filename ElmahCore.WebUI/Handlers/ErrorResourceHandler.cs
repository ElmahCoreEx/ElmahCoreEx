using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ElmahCore.WebUI.Handlers
{
    internal static class ErrorResourceHandler
    {
        private static readonly string[] ResourceNames =
            typeof(ErrorLogWebUiMiddleware).GetTypeInfo().Assembly.GetManifestResourceNames();

        public static async Task ProcessRequest(HttpContext context, string path, string elmahRoot)
        {
            path = path.ToLower();

            var assembly = typeof(ErrorResourceHandler).GetTypeInfo().Assembly;

            var resName = $"{assembly.GetName().Name}.wwwroot.{path.Replace('/', '.').Replace('\\', '.')}";
            if (!path.Contains("."))
            {
                resName = $"{assembly.GetName().Name}.wwwroot.index.html";
                using (var stream2 = assembly.GetManifestResourceStream(resName))
                using (var reader = new StreamReader(stream2 ?? throw new InvalidOperationException()))
                {
                    var html = await reader.ReadToEndAsync();
                    html = html.Replace("ELMAH_ROOT", elmahRoot);
                    context.Response.ContentType = "text/html";
                    await context.Response.WriteAsync(html);
                    return;
                }
            }

            if (!((IList) ResourceNames).Contains(resName))
            {
                context.Response.StatusCode = 404;
                return;
            }

            var ext = Path.GetExtension(path).ToLower();
            context.Response.ContentType = ext switch
            {
                ".svg" => "image/svg+xml",
                ".css" => "text/css",
                ".js" => "text/javascript",
                _ => context.Response.ContentType
            };

            using var resource = assembly.GetManifestResourceStream(resName);
            if (resource != null) await resource.CopyToAsync(context.Response.Body);
        }
    }
}