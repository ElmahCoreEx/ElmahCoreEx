using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace ElmahCore
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class ElmahLogFeature
    {
        private readonly ConcurrentDictionary<Guid, ElmahLogSqlEntry> _map = new();
        public readonly List<ElmahLogMessageEntry> Log = new();
        public readonly List<ElmahLogParameters> Params = new();

        public List<ElmahLogSqlEntry> LogSql => _map.Values.OrderBy(i => i.TimeStamp).ToList();

        public void AddMessage(ElmahLogMessageEntry entry)
        {
            Log.Add(entry);
        }

        public void AddSql(Guid id, ElmahLogSqlEntry entry)
        {
            _map.TryAdd(id, entry);
        }

        public void SetSqlDuration(Guid id)
        {
            if (!_map.TryGetValue(id, out var data)) return;
            data.DurationMs = (int)Math.Round((DateTime.Now - data.TimeStamp).TotalMilliseconds);
        }

        public void LogParameters((string name, object value)[] list, string typeName, string memberName,
            string file, int line)
        {
            Params.Add(new ElmahLogParameters(DateTime.Now, list, typeName, memberName, file, line));
        }
    }
}