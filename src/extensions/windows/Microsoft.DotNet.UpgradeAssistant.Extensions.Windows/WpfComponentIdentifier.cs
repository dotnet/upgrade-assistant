// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows
{
    public class WpfComponentIdentifier : IComponentIdentifier
    {
        private const string DesktopSdk = "Microsoft.NET.Sdk.Desktop";

        private readonly string[] WinFormsFrameworkReferences = new[]
        {
            "Microsoft.WindowsDesktop.App.WindowsForms",
        };

        private readonly string[] WpfFrameworkReferences = new[]
        {
            "Microsoft.WindowsDesktop.App.WPF",
        };

        private readonly string[] DesktopFrameworkReferences = new[]
        {
            "Microsoft.WindowsDesktop.App",
            "Microsoft.WindowsDesktop.App.WindowsForms",
            "Microsoft.WindowsDesktop.App.WPF"
        };

        private readonly string[] WinFormsReferences = new[]
        {
            "System.Windows.Forms"
        };

        private readonly string[] WpfReferences = new[]
        {
            "System.Xaml",
            "PresentationCore",
            "PresentationFramework",
            "WindowsBase"
        };

        private readonly string[] WinRTPackages = new[]
        {
            "Microsoft.Windows.SDK.Contracts"
        };

        public async ValueTask<ProjectComponents> GetComponentsAsync(IProject project, CancellationToken token)
        {
            if (project is null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            var file = project.GetFile();
            var components = ProjectComponents.None;

            if (await IsWinRt(project, token).ConfigureAwait(false))
            {
                components |= ProjectComponents.WinRT;
            }

            var references = project.References.Select(r => r.Name);

            if (references.Any(r => WinFormsReferences.Contains(r, StringComparer.OrdinalIgnoreCase)))
            {
                components |= ProjectComponents.WindowsDesktop;
                components |= ProjectComponents.WinForms;
            }

            if (references.Any(r => WpfReferences.Contains(r, StringComparer.OrdinalIgnoreCase)))
            {
                components |= ProjectComponents.WindowsDesktop;
                components |= ProjectComponents.Wpf;
            }

            if (file.IsSdk)
            {
                if (file.Sdk.Contains(DesktopSdk))
                {
                    components |= ProjectComponents.WindowsDesktop;
                }

                if (file.GetPropertyValue("UseWPF").Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    components |= ProjectComponents.Wpf;
                    components |= ProjectComponents.WindowsDesktop;
                }

                if (file.GetPropertyValue("UseWindowsForms").Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    components |= ProjectComponents.WinForms;
                    components |= ProjectComponents.WindowsDesktop;
                }

                var frameworkReferenceNames = project.FrameworkReferences.Select(r => r.Name);

                if (frameworkReferenceNames.Any(f => DesktopFrameworkReferences.Contains(f, StringComparer.OrdinalIgnoreCase)))
                {
                    components |= ProjectComponents.WindowsDesktop;
                }

                if (frameworkReferenceNames.Any(f => WinFormsReferences.Contains(f, StringComparer.OrdinalIgnoreCase)))
                {
                    components |= ProjectComponents.WinForms;
                }

                if (frameworkReferenceNames.Any(f => WpfReferences.Contains(f, StringComparer.OrdinalIgnoreCase)))
                {
                    components |= ProjectComponents.Wpf;
                }
            }

            return components;
        }

        private ValueTask<bool> IsWinRt(IProject project, CancellationToken token) =>
            WinRTPackages
                .ToAsyncEnumerable()
                .AnyAwaitAsync(package => project.NuGetReferences.IsTransitivelyAvailableAsync(package, token), cancellationToken: token);
    }
}
