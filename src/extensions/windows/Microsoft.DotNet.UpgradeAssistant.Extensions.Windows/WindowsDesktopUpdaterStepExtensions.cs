// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.DotNet.UpgradeAssistant.Dependencies;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Windows;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Windows.UWPtoWinAppSDKUpgrade;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.DotNet.UpgradeAssistant
{
    /// <summary>
    /// Extension class with methods for registering WinformsUpdaterStep-related services.
    /// </summary>
    public static class WindowsDesktopUpdaterStepExtensions
    {
        /// <summary>
        /// Extension method for registering WinformsUpdaterStep and related services.
        /// </summary>
        /// <param name="services">The IServiceCollection to register services in.</param>
        /// <returns>The services argument updated with WinformsUpdaterStep and related services included.</returns>
        public static IServiceCollection AddWinformsUpdaterStep(this IServiceCollection services) =>
            services
                .AddTransient<IUpdater<IProject>, WinformsDefaultFontUpdater>()
                .AddTransient<IUpdater<IProject>, WinformsDpiSettingUpdater>();

        public static IServiceCollection AddWinUIUpdateSteps(this IServiceCollection services) =>
            services
                .AddTransient<DiagnosticAnalyzer, WinUIBackButtonAnalyzer>()
                .AddTransient<CodeFixProvider, WinUIBackButtonCodeFixer>()
                .AddTransient<DiagnosticAnalyzer, WinUIContentDialogAnalyzer>()
                .AddTransient<CodeFixProvider, WinUIContentDialogCodeFixer>()
                .AddTransient<DiagnosticAnalyzer, WinUIInitializeWindowAnalyzer>()
                .AddTransient<CodeFixProvider, WinUIInitializeWindowCodeFixer>()
                .AddTransient<DiagnosticAnalyzer, WinUIDataTransferManagerAnalyzer>()
                .AddTransient<CodeFixProvider, WinUIDataTransferManagerCodeFixer>()
                .AddTransient<DiagnosticAnalyzer, WinUIInteropAnalyzer>()
                .AddTransient<CodeFixProvider, WinUIInteropCodeFixer>()
                .AddTransient<DiagnosticAnalyzer, WinUIMRTResourceManagerAnalyzer>()
                .AddTransient<CodeFixProvider, WinUIMRTResourceManagerCodeFixer>()
                .AddTransient<DiagnosticAnalyzer, WinUIAppWindowAnalyzer>()
                .AddTransient<CodeFixProvider, WinUIAppWindowCodeFixer>()
                .AddTransient<CodeFixProvider, WinUIApiAlertCodeFixer>()
                .AddTransient<IDependencyAnalyzer, WinUIReferenceAnalyzer>()
                .AddTransient<IUpdater<IProject>, WinUINamespaceUpdater>()
                .AddTransient<IUpdater<IProject>, WinUIPropertiesUpdater>()
                .AddTransient<IUpdater<IProject>, WinUIPackageAppxmanifestUpdater>()
                .AddTransient<IUpdater<IProject>, WinUIUnnecessaryFilesUpdater>()
                .AddTransient<IUpdater<IProject>, WinUIAnimationsXamlUpdater>();
    }
}
