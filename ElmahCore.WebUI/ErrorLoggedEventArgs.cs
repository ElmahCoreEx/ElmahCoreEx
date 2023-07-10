﻿using System;

namespace ElmahCore.WebUI
{
    [Serializable]
    public sealed class ErrorLoggedEventArgs : EventArgs
    {
        public ErrorLoggedEventArgs(ErrorLogEntry entry)
        {
            Entry = entry ?? throw new ArgumentNullException(nameof(entry));
        }

        public ErrorLogEntry Entry { get; }
    }
}