using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace ElmahCore.Mvc
{
    public sealed class ElmahDiagnosticObserver : IObserver<DiagnosticListener>
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
            // TODO: Should we really be subscribing in OnNext?  (Logic from original elmah).
            var x = _provider.GetService<IElmahDiagnosticObserver>();
            var diagnosticObservers = _provider.GetServices<IElmahDiagnosticObserver>();
            foreach (var diagnosticObserver in diagnosticObservers)
            {
                var subscription = value.Subscribe(diagnosticObserver);
                _subscriptions.Add(subscription);
            }
        }
    }
}