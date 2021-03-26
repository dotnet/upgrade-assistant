// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using NuGet.Frameworks;

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild
{
    public class TargetFrameworkMonikerCollection : IReadOnlyCollection<TargetFrameworkMoniker>
    {
        private readonly string[] _tfms;

        public TargetFrameworkMonikerCollection(IProjectFile projectFile)
        {
            if (projectFile is null)
            {
                throw new ArgumentNullException(nameof(projectFile));
            }

            _tfms = GetFrameworkMonikers(projectFile);
        }

        public int Count => _tfms.Length;

        public IEnumerator<TargetFrameworkMoniker> GetEnumerator()
        {
            foreach (var tfm in _tfms)
            {
                yield return new TargetFrameworkMoniker(tfm);
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

        private static string[] GetFrameworkMonikers(IProjectFile file)
            => file.IsSdk ? GetSdkTargetFramework(file) : GetNonSdkTargetFramework(file);

        private static string[] GetSdkTargetFramework(IProjectFile file)
        {
            const string SdkSingleTargetFramework = "TargetFramework";
            const string SdkMultipleTargetFrameworks = "TargetFrameworks";

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
