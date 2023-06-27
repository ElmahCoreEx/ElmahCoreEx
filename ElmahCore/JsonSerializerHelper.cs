using System.Text.Json;

namespace ElmahCore
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