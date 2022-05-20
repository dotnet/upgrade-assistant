// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.UpgradeAssistant.Dependencies;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows
{
    public class WindowsComponentIdentifier : IComponentIdentifier
    {
        private const string DesktopSdk = "Microsoft.NET.Sdk.Desktop";
        private const string WinRTPackage = "Microsoft.Windows.SDK.Contracts";
        private const string WinAppSDKPackage = "Microsoft.WindowsAppSDK";

        private readonly string[] _desktopFrameworkReferences = new[]
        {
            "Microsoft.WindowsDesktop.App",
            "Microsoft.WindowsDesktop.App.WindowsForms",
            "Microsoft.WindowsDesktop.App.WPF"
        };

        private readonly string[] _winFormsReferences = new[]
        {
            "System.Windows.Forms"
        };

        private readonly string[] _wpfReferences = new[]
        {
            "System.Xaml",
            "PresentationCore",
            "PresentationFramework",
            "WindowsBase"
        };

        private readonly string[] _uwpPackageReferences = new[]
        {
            "Microsoft.NETCore.UniversalWindowsPlatform"
        };

        private readonly ITransitiveDependencyIdentifier _identifier;

        public WindowsComponentIdentifier(ITransitiveDependencyIdentifier identifier)
        {
            _identifier = identifier;
        }

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
            var packageReferences = project.PackageReferences.Select(r => r.Name);

            if (references.Any(r => _winFormsReferences.Contains(r, StringComparer.OrdinalIgnoreCase)))
            {
                components |= ProjectComponents.WindowsDesktop;
                components |= ProjectComponents.WinForms;
            }

            if (references.Any(r => _wpfReferences.Contains(r, StringComparer.OrdinalIgnoreCase)))
            {
                components |= ProjectComponents.WindowsDesktop;
                components |= ProjectComponents.Wpf;
            }

            if (packageReferences.Any(p => _uwpPackageReferences.Contains(p, StringComparer.OrdinalIgnoreCase)))
            {
                components |= ProjectComponents.WindowsDesktop;
                components |= ProjectComponents.WinUI;
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

                if (file.GetPropertyValue("UseWinUI").Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    components |= ProjectComponents.WinUI;
                    components |= ProjectComponents.WindowsDesktop;
                }

                var frameworkReferenceNames = project.FrameworkReferences.Select(r => r.Name);

                if (frameworkReferenceNames.Any(f => _desktopFrameworkReferences.Contains(f, StringComparer.OrdinalIgnoreCase)))
                {
                    components |= ProjectComponents.WindowsDesktop;
                }

                if (frameworkReferenceNames.Any(f => _winFormsReferences.Contains(f, StringComparer.OrdinalIgnoreCase)))
                {
                    components |= ProjectComponents.WinForms;
                }

                if (frameworkReferenceNames.Any(f => _wpfReferences.Contains(f, StringComparer.OrdinalIgnoreCase)))
                {
                    components |= ProjectComponents.Wpf;
                }
            }

            return components;
        }

        private async ValueTask<bool> IsWinRt(IProject project, CancellationToken token)
        {
            return (await _identifier.IsTransitiveDependencyAsync(WinRTPackage, project, token).ConfigureAwait(false))
                || (await _identifier.IsTransitiveDependencyAsync(WinAppSDKPackage, project, token).ConfigureAwait(false));
        }
    }
}
