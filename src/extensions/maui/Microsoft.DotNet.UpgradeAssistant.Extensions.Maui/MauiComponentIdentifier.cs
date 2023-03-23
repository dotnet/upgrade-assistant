// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Maui
{
    public class MauiComponentIdentifier : IComponentIdentifier
    {
        private const string _useMauiProperty = "UseMaui";
        private const string _xamarinFormsPackage = "Xamarin.Forms";
        private static readonly string[] _xamarinAndroidReferences = new[] { "Mono.Android" };
        private static readonly string[] _xamariniOSReferences = new[] { "Xamarin.iOS" };

        public ValueTask<ProjectComponents> GetComponentsAsync(IProject project, CancellationToken token)
        {
            if (project is null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            var file = project.GetFile();

            var components = ProjectComponents.None;

            var references = project.References.Select(r => r.Name);

            foreach (var reference in references)
            {
                if (_xamarinAndroidReferences.Contains(reference, StringComparer.OrdinalIgnoreCase))
                {
                    components |= ProjectComponents.XamarinAndroid;
                }
                else if (_xamariniOSReferences.Contains(reference, StringComparer.OrdinalIgnoreCase))
                {
                    components |= ProjectComponents.XamariniOS;
                }
            }

            if (project.NuGetReferences.PackageReferences.Any(x => x.Name.Equals(_xamarinFormsPackage, StringComparison.OrdinalIgnoreCase)) && project.IsNetStandard())
            {
                components |= ProjectComponents.Maui;
            }

            if (project.GetProjectPropertyElements().GetProjectPropertyValue(_useMauiProperty).Any())
            {
                components |= ProjectComponents.Maui;
            }

            // Check if there are any platforms specified in the TFMs. This will only be .NET 6+, which implies Maui and not Xamarin
            foreach (var tfm in project.TargetFrameworks)
            {
                if (tfm.IsPlatform(TargetFrameworkMoniker.Platforms.Android))
                {
                    components |= ProjectComponents.MauiAndroid;
                }
                else if (tfm.IsPlatform(TargetFrameworkMoniker.Platforms.IOS))
                {
                    components |= ProjectComponents.MauiiOS;
                }
            }

            return new ValueTask<ProjectComponents>(components);
        }
    }
}
