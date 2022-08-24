// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.UpgradeAssistant
{
    public static class WellKnownStepIds
    {
        public const string BackupStepId = "Microsoft.DotNet.UpgradeAssistant.Steps.Backup.BackupStep";
        public const string ConfigUpdaterStepId = "Microsoft.DotNet.UpgradeAssistant.Steps.Configuration.ConfigUpdaterStep";
        public const string PackageUpdaterPreTFMStepId = "Microsoft.DotNet.UpgradeAssistant.Steps.Packages.PackageUpdaterPreTFMStep";
        public const string PackageUpdaterStepId = "Microsoft.DotNet.UpgradeAssistant.Steps.Packages.PackageUpdaterStep";
        public const string TryConvertProjectConverterStepId = "Microsoft.DotNet.UpgradeAssistant.Steps.ProjectFormat.TryConvertProjectConverterStep";
        public const string SetTFMStepId = "Microsoft.DotNet.UpgradeAssistant.Steps.ProjectFormat.SetTFMStep";
        public const string CurrentProjectSelectionStepId = "Microsoft.DotNet.UpgradeAssistant.Steps.Solution.CurrentProjectSelectionStep";
        public const string EntrypointSelectionStepId = "Microsoft.DotNet.UpgradeAssistant.Steps.Solution.EntrypointSelectionStep";
        public const string NextProjectStepId = "Microsoft.DotNet.UpgradeAssistant.Steps.Solution.NextProjectStep";
        public const string FinalizeSolutionStepId = "Microsoft.DotNet.UpgradeAssistant.Steps.Solution.FinalizeSolutionStep";
        public const string SourceUpdaterStepId = "Microsoft.DotNet.UpgradeAssistant.Steps.Source.SourceUpdaterStep";
        public const string RazorUpdaterStepId = "Microsoft.DotNet.UpgradeAssistant.Steps.Razor.RazorUpdaterStep";
        public const string TemplateInserterStepId = "Microsoft.DotNet.UpgradeAssistant.Steps.Templates.TemplateInserterStep";
        public const string VisualBasicProjectUpdaterStepId = "Microsoft.DotNet.UpgradeAssistant.Extensions.VisualBasic.VisualBasicProjectUpdaterStep";
        public const string WindowsDesktopUpdateStepId = "Microsoft.DotNet.UpgradeAssistant.Extensions.Windows.WindowsDesktopUpdateStep";
        public const string WCFUpdateStepId = "Microsoft.DotNet.UpgradeAssistant.Extensions.WCFUpdater.WCFUpdateStep";
    }
}
