﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using NuGet.Frameworks;

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild
{
    public class TargetFrameworkMonikerCollection : IReadOnlyCollection<TargetFrameworkMoniker>
    {
        private const string SdkSingleTargetFramework = "TargetFramework";
        private const string SdkMultipleTargetFrameworks = "TargetFrameworks";

        private readonly IProjectFile _projectFile;

        private string[]? _tfms;

        public TargetFrameworkMonikerCollection(IProjectFile projectFile)
        {
            if (projectFile is null)
            {
                throw new ArgumentNullException(nameof(projectFile));
            }

            _projectFile = projectFile;
        }

        public void SetTargetFramework(TargetFrameworkMoniker tfm)
        {
            if (tfm is null)
            {
                throw new ArgumentNullException(nameof(tfm));
            }

            if (!_projectFile.IsSdk)
            {
                throw new InvalidOperationException("Project file only supports setting TFM on new style csproj");
            }

            _tfms = null;

            if (!string.IsNullOrWhiteSpace(_projectFile.GetPropertyValue(SdkSingleTargetFramework)))
            {
                _projectFile.SetPropertyValue(SdkSingleTargetFramework, tfm.Name);
            }
            else if (!string.IsNullOrWhiteSpace(_projectFile.GetPropertyValue(SdkMultipleTargetFrameworks)))
            {
                _projectFile.SetPropertyValue(SdkMultipleTargetFrameworks, tfm.Name);
            }
            else
            {
                throw new InvalidOperationException("Could not find existing TFM node.");
            }
        }

        private string[] RawValues
        {
            get
            {
                if (_tfms is null)
                {
                    _tfms = GetFrameworkMonikers(_projectFile);
                }

                return _tfms;
            }
        }

        public int Count => RawValues.Length;

        public IEnumerator<TargetFrameworkMoniker> GetEnumerator()
        {
            foreach (var tfm in RawValues)
            {
                yield return new TargetFrameworkMoniker(tfm);
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

        private static string[] GetFrameworkMonikers(IProjectFile file)
            => file.IsSdk ? GetSdkTargetFramework(file) : GetNonSdkTargetFramework(file);

        private static string[] GetSdkTargetFramework(IProjectFile file)
        {
            var single = GetTfms(file, SdkSingleTargetFramework);

            if (single.Length > 0)
            {
                return single;
            }

            return GetTfms(file, SdkMultipleTargetFrameworks);
        }

        private static string[] GetNonSdkTargetFramework(IProjectFile file)
        {
            const string NonSdkTargetFramework = "TargetFrameworkVersion";

            var tfms = GetTfms(file, NonSdkTargetFramework);

            for (var i = 0; i < tfms.Length; i++)
            {
                var version = Version.Parse(tfms[i].Trim('v', 'V'));
                var framework = new NuGetFramework(FrameworkConstants.FrameworkIdentifiers.Net, version);
                tfms[i] = framework.GetShortFolderName();
            }

            return tfms;
        }

        private static string[] GetTfms(IProjectFile file, string propertyName)
            => file.GetPropertyValue(propertyName).Split(';', StringSplitOptions.RemoveEmptyEntries);
    }
}
