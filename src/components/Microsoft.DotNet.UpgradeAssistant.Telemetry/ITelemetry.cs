// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
