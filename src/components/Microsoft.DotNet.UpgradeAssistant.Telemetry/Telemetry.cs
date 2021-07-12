// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.DotNet.PlatformAbstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant.Telemetry
{
    [SuppressMessage("Naming", "CA1724: Type names should not match namespaces", Justification = "Keeping it consistent with source implementations.")]
    internal sealed class Telemetry : ITelemetry
    {
        private readonly TelemetryOptions _options;
        private readonly TelemetryConfiguration? _telemetryConfig;
        private readonly TelemetryClient? _client;

        public Telemetry(
            IOptions<TelemetryOptions> options,
            IEnumerable<ITelemetryInitializer> initializers,
            IFirstTimeUseNoticeSentinel sentinel)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _options = options.Value;

            Enabled = _options.IsEnabled && !EnvironmentHelper.GetEnvironmentVariableAsBool(_options.TelemetryOptout) && PermissionExists(sentinel);

            if (!Enabled)
            {
                return;
            }

            _telemetryConfig = TelemetryConfiguration.CreateDefault();

            foreach (var initializer in initializers)
            {
                _telemetryConfig.TelemetryInitializers.Add(initializer);
            }

            _client = new TelemetryClient(_telemetryConfig)
            {
                InstrumentationKey = _options.InstrumentationKey
            };

            _client.Context.Session.Id = _options.CurrentSessionId;
            _client.Context.Device.OperatingSystem = RuntimeEnvironment.OperatingSystem;
        }

        public bool Enabled { get; }

        private static bool PermissionExists(IFirstTimeUseNoticeSentinel? sentinel)
        {
            if (sentinel == null)
            {
                return false;
            }

            return sentinel.Exists();
        }

        public void TrackException(Exception exception, IDictionary<string, string>? properties, IDictionary<string, double>? measurements)
        {
            if (!Enabled || _client is null)
            {
                return;
            }

            _client.TrackException(exception, properties, measurements);
        }

        public void TrackEvent(string eventName, IDictionary<string, string>? properties, IDictionary<string, double>? measurements)
        {
            if (!Enabled || _client is null)
            {
                return;
            }

            _client.TrackEvent(PrependProducerNamespace(eventName), properties, measurements);
        }

        private string PrependProducerNamespace(string eventName) => $"{_options.ProducerNamespace}/{eventName}";

        public void Dispose()
        {
            _telemetryConfig?.Dispose();

            if (_client is not null)
            {
                _client.Flush();
            }
        }
    }
}
