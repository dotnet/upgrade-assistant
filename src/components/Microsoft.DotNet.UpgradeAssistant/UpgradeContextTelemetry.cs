// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.DotNet.UpgradeAssistant.Telemetry;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public class UpgradeContextTelemetry : ITelemetryInitializer, IUpgradeContextAccessor
    {
        private readonly AsyncLocal<IUpgradeContext?> _context = new AsyncLocal<IUpgradeContext?>();
        private readonly IStringHasher _hasher;

        public UpgradeContextTelemetry(IStringHasher hasher)
        {
            _hasher = hasher;
        }

        public IUpgradeContext? Current
        {
            get => _context.Value;
            set => _context.Value = value;
        }

        public void Initialize(ApplicationInsights.Channel.ITelemetry telemetry)
        {
            if (telemetry is not ISupportProperties t)
            {
                return;
            }

            if (Current is not IUpgradeContext context)
            {
                return;
            }

            // Solution and project ids are already hashed and so don't need to deal with that
            TryAdd(t.Properties, "Solution Id", context.SolutionId);
            TryAdd(t.Properties, "Project Id", context.CurrentProject?.Id);

            // StepIds should be hashed as they can potentially be extended by external users
            if (context.CurrentStep?.Id is string stepId)
            {
                TryAdd(t.Properties, "Step Id", _hasher.Hash(stepId));
            }

            static void TryAdd(IDictionary<string, string> dict, string name, string? value)
            {
                if (value is not null && !dict.ContainsKey(name))
                {
                    dict.Add(name, value);
                }
            }
        }
    }
}
