// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.XPath;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows
{
    internal static class WindowsUtilities
    {
        public static async ValueTask<bool> IsWinFormsProjectAsync(this IProject project, CancellationToken token)
        {
            var components = await project.GetComponentsAsync(token).ConfigureAwait(false);
            return components.HasFlag(ProjectComponents.WinForms);
        }

        public static async ValueTask<bool> IsWinUIProjectAsync(this IProject project, CancellationToken token)
        {
            var components = await project.GetComponentsAsync(token).ConfigureAwait(false);
            return components.HasFlag(ProjectComponents.WinUI);
        }

        public static string GetElementFromAppConfig(string configPath, string configuration, string key)
        {
            if (File.Exists(configPath))
            {
                var configFile = new ConfigFile(configPath);
                var configElement = configFile.Contents.XPathSelectElement(configuration);
                if (configElement is not null)
                {
                    var hdpi = configElement.Elements("add").Where(e => e.Attribute("key").Value == key).FirstOrDefault();
                    if (hdpi is not null)
                    {
                        return hdpi.Attribute("value").Value;
                    }
                }
            }

            return string.Empty;
        }
    }
}
