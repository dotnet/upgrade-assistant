// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Maui
{
    /// <summary>
    /// Users using Xamarin.Forms lower than version 4.8 should be informed to update to a higher version 
    /// before they run this tool.
    /// </summary>
    public class XamarinFormsCheck : IUpgradeReadyCheck
    {
        private const string CATEGORY = ".NET MAUI Upgrade";
        private const string UPGRADE_LINK = "https://aka.ms/upgradeassistant/maui/requirements";
        private const string XAMARIN_FORMS_MESSAGE = "Support for {0} is limited to Xamarin.Forms version 4.8 or higher. To learn more please read: {1}";
        private const double MINIMUM_XAMARIN_VERSION = 4.8;

        private readonly ILogger<XamarinFormsCheck> _logger;

        public XamarinFormsCheck(ILogger<XamarinFormsCheck> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string Id => "Xamarin.Forms minimum version check";

        public string UpgradeMessage => string.Format(CultureInfo.InvariantCulture, XAMARIN_FORMS_MESSAGE, CATEGORY, UPGRADE_LINK);

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
                if (!DoesHaveMinimumXamarinVersion(project))
                {
                    _logger.LogError(XAMARIN_FORMS_MESSAGE, CATEGORY, UPGRADE_LINK);
                    return UpgradeReadiness.Unsupported;
                }
            }

            return UpgradeReadiness.Ready;
        }

        private static bool DoesHaveMinimumXamarinVersion(IProject project)
        {
            var packageReferences = project.NuGetReferences.PackageReferences;

            foreach (var package in packageReferences)
            {
                if (package.Name == "Xamarin.Forms")
                {
                    return Convert.ToDouble(package.Version.Substring(0, 3), CultureInfo.InvariantCulture) >= MINIMUM_XAMARIN_VERSION;
                }
            }

            return false;
        }
    }
}
