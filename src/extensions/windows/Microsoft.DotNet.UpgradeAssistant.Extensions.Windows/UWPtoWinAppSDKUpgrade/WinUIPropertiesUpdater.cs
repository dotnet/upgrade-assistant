// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Windows.UWPtoWinAppSDKUpgrade.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows
{
    [ApplicableComponents(ProjectComponents.WinUI)]
    public class WinUIPropertiesUpdater : IUpdater<IProject>
    {
        public const string RuleID = "UA302";

        private const string CsWinRTLogMessageFormat = "A CsWinRTIncludes property with value {0} has been added to specify the namespace of the referenced vcxproj component to project..\n" +
                            "If your project assembly name differs from {0}, update this value with the assembly name.\n" +
                            "Read more about C#/WinRT here: https://docs.microsoft.com/en-us/windows/apps/develop/platform/csharp-winrt/";

        private const string CsWinRTIncludesProperty = "CsWinRTIncludes";

        public string Id => typeof(WinUIPropertiesUpdater).FullName;

        public string Title => "Update WinUI Project Properties";

        public string Description => "Update project properties to use WinUI and Windows App SDK.";

        public BuildBreakRisk Risk => BuildBreakRisk.Medium;

        private readonly ILogger<WinUIPropertiesUpdater> _logger;

        private readonly WinUIOptionsProjectFilePropertyUpdates? _projectFilePropertyUpdates;

        public WinUIPropertiesUpdater(ILogger<WinUIPropertiesUpdater> logger, IOptions<WinUIOptions> options)
        {
            this._logger = logger;
            this._projectFilePropertyUpdates = options?.Value.ProjectFilePropertyUpdates;
        }

        public async Task<IUpdaterResult> ApplyAsync(IUpgradeContext context, ImmutableArray<IProject> inputs, CancellationToken token)
        {
            if (this._projectFilePropertyUpdates == null)
            {
                return new WindowsDesktopUpdaterResult(
                    RuleID,
                    RuleName: Id,
                    FullDescription: Title,
                    false,
                    string.Empty,
                    new List<string>());
            }

            foreach (var project in inputs)
            {
                var projectFile = project.GetFile();

                if (_projectFilePropertyUpdates.Set != null)
                {
                    foreach (var propToSet in _projectFilePropertyUpdates.Set)
                    {
                        projectFile.SetPropertyValue(propToSet.Key, propToSet.Value);
                    }
                }

                if (_projectFilePropertyUpdates.Remove != null)
                {
                    foreach (var propToRemove in _projectFilePropertyUpdates.Remove)
                    {
                        projectFile.RemoveProperty(propToRemove);
                    }
                }

                foreach (var projRef in project.AllProjectReferences())
                {
                    if (projRef.Contains(".vcxproj"))
                    {
                        var projectName = ParseProjectNameWithExtension(projRef, ".vcxproj");
                        var csWinRTIncludesValue = projectFile.GetPropertyValue(CsWinRTIncludesProperty) ?? string.Empty;
                        var delimiter = csWinRTIncludesValue.Trim().Length == 0 || csWinRTIncludesValue.EndsWith(";") ? string.Empty : ";";
                        projectFile.SetPropertyValue(CsWinRTIncludesProperty, $"{csWinRTIncludesValue}{delimiter}{projectName}");

                        _logger.LogInformation(string.Format(CsWinRTLogMessageFormat, projectName));
                    }
                }

                projectFile.AddItem(new ProjectItemDescriptor(ProjectItemType.Compile) { Remove = "App.xaml.old.cs" });
                projectFile.AddItem(new ProjectItemDescriptor(ProjectItemType.None) { Include = "App.xaml.old.cs" });
                projectFile.RemoveItem(new ProjectItemDescriptor(ProjectItemType.Content) { Include = "Properties\\Default.rd.xml" });

                await projectFile.SaveAsync(token).ConfigureAwait(false);
            }

            return new WindowsDesktopUpdaterResult(
                RuleID,
                RuleName: Id,
                FullDescription: Title,
                true,
                string.Empty,
                new List<string>());
        }

        /*
          This function parses the project name from projectReference string which includes the file path, project id and more text.
          It does so by finding the position of ".vcxproj" in the string and then reading the name backwards character by character
          until the first non-alphanumeric character.
        */
        private string ParseProjectNameWithExtension(string projectReference, string extension)
        {
            var index = projectReference.IndexOf(".vcxproj");
            index--;
            StringBuilder sb = new StringBuilder();
            while (index > -1 && char.IsLetterOrDigit(projectReference[index]))
            {
                sb.Append(projectReference[index]);
                index--;
            }

            return new string(sb.ToString().Reverse().ToArray());
        }

        public async Task<IUpdaterResult> IsApplicableAsync(IUpgradeContext context, ImmutableArray<IProject> inputs, CancellationToken token)
        {
            await Task.Yield();
            if (this._projectFilePropertyUpdates == null)
            {
                return new WindowsDesktopUpdaterResult(
                    RuleID,
                    RuleName: Id,
                    FullDescription: Title,
                    false,
                    string.Empty,
                    ImmutableList.Create<string>());
            }

            return new WindowsDesktopUpdaterResult(
                RuleID,
                RuleName: Id,
                FullDescription: Title,
                true,
                string.Empty,
                ImmutableList.Create<string>());
        }
    }
}
