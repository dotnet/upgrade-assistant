// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Maui
{
    public class MauiComponentIdentifier : IComponentIdentifier
    {
        private readonly string[] _xamarinAndroidReferences = new[] { "Mono.Android" };
        private readonly string[] _xamariniOSReferences = new[] { "Xamarin.iOS" };
        private readonly string[] _androidTFM = new[] { TargetFrameworkMoniker.Net60_Android.ToString() };
        private readonly string[] _iosTFM = new[] { TargetFrameworkMoniker.Net60_iOS.ToString() };

        public ValueTask<ProjectComponents> GetComponentsAsync(IProject project, CancellationToken token)
        {
            if (project is null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            var file = project.GetFile();
            var components = ProjectComponents.None;

            var references = project.References.Select(r => r.Name);

            if (references.Any(r => _xamarinAndroidReferences.Contains(r, StringComparer.OrdinalIgnoreCase)))
            {
                components |= ProjectComponents.XamarinAndroid;
            }

            if (references.Any(r => _xamariniOSReferences.Contains(r, StringComparer.OrdinalIgnoreCase)))
            {
                components |= ProjectComponents.XamariniOS;
            }

            var targetFrameworkNames = project.TargetFrameworks.Select(r => r.Name);

            if (targetFrameworkNames.Any(r => _androidTFM.Contains(r, StringComparer.OrdinalIgnoreCase)))
            {
                components |= ProjectComponents.MauiAndroid;
            }

            if (targetFrameworkNames.Any(r => _iosTFM.Contains(r, StringComparer.OrdinalIgnoreCase)))
            {
                components |= ProjectComponents.MauiiOS;
            }

            return new ValueTask<ProjectComponents>(components);
        }
    }
}
