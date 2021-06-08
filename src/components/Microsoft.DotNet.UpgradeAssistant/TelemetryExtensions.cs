// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Telemetry;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public static class TelemetryExtensions
    {
        public static IDisposable TimeStep(this ITelemetry telemetry, string eventName, UpgradeStep step)
            => telemetry.TimeEvent($"step/{eventName}", onComplete: (p, _) => p.Add("Step Status", step.Status.ToString()));

        public static async Task TrackProjectPropertiesAsync(this ITelemetry telemetry, IUpgradeContext context, CancellationToken token)
        {
            if (telemetry is null)
            {
                throw new ArgumentNullException(nameof(telemetry));
            }

            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!telemetry.Enabled)
            {
                return;
            }

            foreach (var project in context.Projects)
            {
                try
                {
                    var components = await project.GetComponentsAsync(token).ConfigureAwait(false);
                    var properties = new Dictionary<string, string>
                    {
                        { "Project Id", project.Id },
                        { "Output Type", project.OutputType.ToString() },
                        { "Target Frameworks", string.Join(";", project.TargetFrameworks.Select(t => t.Name)) },
                        { "Components", components.ToString() },
                        { "Project Types", string.Join(";", project.ProjectTypes) },
                    };

                    telemetry.TrackEvent("project", properties);
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception e)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    telemetry.TrackEvent("project/error", new Dictionary<string, string> { { "message", e.Message } });
                }
            }
        }
    }
}
