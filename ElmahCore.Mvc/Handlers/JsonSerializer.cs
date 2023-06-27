using System.Text.Json;

namespace ElmahCore.Mvc.Handlers
{
    public static class JsonSerializerHelper
    {
        public static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new JsonSerializerOptions
        {
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            MaxDepth = 0
        };
    }
}