// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.UpgradeAssistant.Telemetry
{
    public interface ITelemetry : IDisposable
    {
        bool Enabled { get; }

        void TrackEvent(string eventName, IDictionary<string, string>? properties = null, IDictionary<string, double>? measurements = null);

        void TrackException(Exception exception, IDictionary<string, string>? properties = null, IDictionary<string, double>? measurements = null);
    }
}
