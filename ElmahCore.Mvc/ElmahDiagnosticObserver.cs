using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ElmahCore.Mvc
{
    internal sealed class ElmahDiagnosticObserver : IObserver<DiagnosticListener>
    {
        private readonly IServiceProvider _provider;
        private readonly List<IDisposable> _subscriptions = new();

        public ElmahDiagnosticObserver(IServiceProvider provider)
        {
            _provider = provider;
        }

        public void OnCompleted()
        {
            _subscriptions.ForEach(x => x.Dispose());
            _subscriptions.Clear();
        }

        public void OnError(Exception error)
        {
        }


        public void OnNext(DiagnosticListener value)
        {
            if (value.Name != "SqlClientDiagnosticListener") return;

            var subscription = value.Subscribe(new ElmahDiagnosticSqlObserver(_provider));
            _subscriptions.Add(subscription);
        }
    }
}