// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.DotNet.UpgradeAssistant.Telemetry
{
    public static class TelemetryExtensions
    {
        public static IDisposable TimeEvent(this ITelemetry telemetry, string eventName, Dictionary<string, string>? properties = null, Dictionary<string, double>? measurements = null, Action<Dictionary<string, string>, Dictionary<string, double>>? onComplete = null)
            => new TimedEvent(telemetry, eventName, properties, measurements, onComplete);

        private class TimedEvent : IDisposable
        {
            private readonly ITelemetry _telemetry;
            private readonly string _eventName;
            private readonly Dictionary<string, string>? _properties;
            private readonly Dictionary<string, double>? _measurements;
            private readonly Action<Dictionary<string, string>, Dictionary<string, double>>? _onComplete;
            private readonly Stopwatch _stopwatch;

            public TimedEvent(ITelemetry telemetry, string eventName, Dictionary<string, string>? properties, Dictionary<string, double>? measurements, Action<Dictionary<string, string>, Dictionary<string, double>>? onComplete)
            {
                _telemetry = telemetry;
                _eventName = eventName;
                _properties = properties;
                _measurements = measurements;
                _onComplete = onComplete;
                _stopwatch = Stopwatch.StartNew();
            }

            public void Dispose()
            {
                _stopwatch.Stop();

                var measurements = _measurements ?? new Dictionary<string, double>();
                var properties = _properties ?? new Dictionary<string, string>();

                measurements.Add("Duration", _stopwatch.ElapsedTicks);

                if (_onComplete is not null)
                {
                    _onComplete(properties, measurements);
                }

                _telemetry.TrackEvent(_eventName, properties, measurements);
            }
        }
    }
}
