using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ElmahCore.Mvc.Notifiers;
using Microsoft.AspNetCore.Http;

namespace ElmahCore.Mvc.Handlers
{
    internal static class ErrorApiHandler
    {
     
        public static async Task ProcessRequest(HttpContext context, ErrorLog errorLog, string path)
        {
            switch (path)
            {
                case "api/error":
                    var errorId = context.Request.Query["id"].ToString();
                    if (string.IsNullOrEmpty(errorId)) await context.Response.WriteAsync("{}");
                    var error = await GetErrorAsync(errorLog, errorId);
                    await context.Response.WriteJsonAsync(JsonSerializer.Serialize(error, JsonSerializerHelper.DefaultJsonSerializerOptions));
                    break;

                case "api/errors":
                    int.TryParse(context.Request.Query["i"].ToString(), out var errorIndex);
                    int.TryParse(context.Request.Query["s"].ToString(), out var pageSize);
                    var entities = await GetErrorsAsync(errorLog, errorIndex, pageSize);
                    await context.Response.WriteJsonAsync(JsonSerializer.Serialize(entities, JsonSerializerHelper.DefaultJsonSerializerOptions));
                    break;

                case "api/new-errors":
                    var id = context.Request.Query["id"].ToString();
                    var newEntities = await GetNewErrorsAsync(errorLog, id);
                    await context.Response.WriteJsonAsync(JsonSerializer.Serialize(newEntities, JsonSerializerHelper.DefaultJsonSerializerOptions));
                    break;
            }
        }

        private static async Task WriteJsonAsync(this HttpResponse response, string json, string contentType = null)
        {
            response.ContentType = contentType ?? "application/json; charset=UTF-8";
            await response.WriteAsync(json);
            await response.Body.FlushAsync();
        }


        private static async Task<ErrorLogEntryWrapper> GetErrorAsync(ErrorLog errorLog, string id)
        {
            var error = await errorLog.GetErrorAsync(id);
            return error == null ? null : new ErrorLogEntryWrapper(error);
        }

        private static async Task<ErrorsList> GetErrorsAsync(ErrorLog errorLog, int errorIndex, int pageSize)
        {
            if (errorIndex < 0) errorIndex = 0;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var entries = new List<ErrorLogEntry>(pageSize);
            var totalCount = await errorLog.GetErrorsAsync(errorIndex, pageSize, entries);
            return new ErrorsList
            {
                Errors = entries.Select(i => new ErrorLogEntryWrapper(i)).ToList(),
                TotalCount = totalCount
            };
        }

        private static async Task<ErrorsList> GetNewErrorsAsync(ErrorLog errorLog, string id)
        {
            if (string.IsNullOrEmpty(id)) return await GetErrorsAsync(errorLog, 0, 50);
            var entries = new List<ErrorLogEntryWrapper>();
            await foreach (var item in errorLog.GetNewErrorsAsync(id))
            {
                entries.Add(new ErrorLogEntryWrapper(item));
            }

            return new ErrorsList
            {
                Errors = entries,
                TotalCount = entries.Count
            };
        }
    }
}