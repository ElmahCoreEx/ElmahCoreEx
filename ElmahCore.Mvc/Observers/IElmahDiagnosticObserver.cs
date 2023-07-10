using System;
using System.Collections.Generic;

namespace ElmahCore.Mvc;

public interface IElmahDiagnosticObserver : IObserver<KeyValuePair<string, object>>
{
}
