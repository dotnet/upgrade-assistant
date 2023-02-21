// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Maui
{
    /// <summary>
    /// Users using Xamarin.Forms lower than version 4.8 should be informed to update to a higher version
    /// before they run this tool.
    /// </summary>
    public class XamarinFormsVersionCheck : IUpgradeReadyCheck
    {
        private const string CATEGORY = ".NET MAUI Upgrade";
        private const string UPGRADE_LINK = "https://aka.ms/upgradeassistant/maui/requirements";
        private const string XAMARIN_FORMS_MESSAGE = "Support for upgrading to {0} is limited to Xamarin.Forms version 4.8 or higher. To learn more please read: {1}";
        private const string MINIMUM_XAMARIN_VERSION = "4.8";

        private readonly ILogger<XamarinFormsVersionCheck> _logger;
        private readonly IVersionComparer _comparer;

        public XamarinFormsVersionCheck(IVersionComparer comparer, ILogger<XamarinFormsVersionCheck> logger)
        {
            _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string Id => "Xamarin.Forms minimum version check";

        public string UpgradeMessage => SR.Format(XAMARIN_FORMS_MESSAGE, CATEGORY, UPGRADE_LINK);

        public async Task<UpgradeReadiness> IsReadyAsync(IProject project, UpgradeReadinessOptions options, CancellationToken token)
        {
            if (project is null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var components = await project.GetComponentsAsync(token).ConfigureAwait(false);
            if (components.HasFlag(ProjectComponents.XamarinAndroid) || components.HasFlag(ProjectComponents.XamariniOS))
            {
                if (!DoesHaveMinimumXamarinVersion(project, _comparer))
                {
                    _logger.LogError(XAMARIN_FORMS_MESSAGE, CATEGORY, UPGRADE_LINK);
                    return UpgradeReadiness.Unsupported;
                }
            }

            return UpgradeReadiness.Ready;
        }

        private static bool DoesHaveMinimumXamarinVersion(IProject project, IVersionComparer comparer)
        {
            var packageReferences = project.NuGetReferences.PackageReferences;
            foreach (var package in packageReferences)
            {
                if (package.Name == "Xamarin.Forms")
                {
                    return comparer.Compare(package.Version, MINIMUM_XAMARIN_VERSION) > 0;
                }
            }

            return true;
        }
    }
}
