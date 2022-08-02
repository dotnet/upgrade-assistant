using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using MSBuild.Conversion.Facts;

using Newtonsoft.Json.Linq;

namespace MSBuild.Abstractions
{
    public static class ProjectExtensions
    {
        public static void LogProjectProperties(this IProject project, string logFileName)
        {
            var lines = new List<string>();
            foreach (var prop in project.Properties.OrderBy(p => p.Name))
            {
                lines.Add($"{prop.Name} = {prop.EvaluatedValue}");
            }
            File.WriteAllLines(logFileName, lines);
        }

        public static string GetTargetFramework(this IProject project)
        {
            var tf = project.GetPropertyValue(MSBuildFacts.TargetFrameworkNodeName);
            if (!string.IsNullOrWhiteSpace(tf))
            {
                return tf;
            }

            var tfi = project.GetPropertyValue(MSBuildFacts.LegacyTargetFrameworkPropertyNodeName);
            if (string.IsNullOrWhiteSpace(tfi))
            {
                // For some befuddling reason, legacy F# projects don't actually have a TFI.
                // We just assume it's .NET Framework though, because like, that's just what it's gonna be anyways.
                tfi = ".NETFramework";
            }

            var tfv = project.GetPropertyValue(MSBuildFacts.LegacyTargetFrameworkVersionNodeName);

            tf = tfi switch
            {
                ".NETFramework" => "net",
                ".NETStandard" => "netstandard",
                ".NETCore" => "net", // UWP
                ".NETCoreApp" => "netcoreapp",
                ".NETPortable" => "netstandard",
                "MonoAndroid" => "net",
                "Xamarin.iOS" => "net",
                _ => throw new InvalidOperationException($"Unknown {MSBuildFacts.LegacyTargetFrameworkPropertyNodeName}: {tfi}"),
            };

            if (tfi == ".NETCore")
            {
                var targetPlatformIdentifier = project.GetPropertyValue(MSBuildFacts.TargetPlatformIdentifierNodeName);
                if (targetPlatformIdentifier.Equals(MSBuildFacts.UapValue, StringComparison.OrdinalIgnoreCase))
                {
                    var targetPlatformVersion = project.GetPropertyValue(MSBuildFacts.TargetPlatformVersionNodeName);
                    if (!string.IsNullOrWhiteSpace(targetPlatformVersion))
                    {
                        tf = $"net6.0-windows{targetPlatformVersion}";
                        return tf;
                    }
                }
            }

            if (tfi.Equals(MSBuildFacts.NETPortableTFValuePrefix, StringComparison.OrdinalIgnoreCase))
            {
                var profile = project.GetPropertyValue(MSBuildFacts.LegacyTargetFrameworkProfileNodeName);

                if (string.IsNullOrWhiteSpace(profile) && tfv.Equals(MSBuildFacts.PCLv5value, StringComparison.OrdinalIgnoreCase))
                {
                    tf = GetTargetFrameworkFromProjectJson(project);
                }
                else
                {
                    var netstandardVersion = MSBuildFacts.PCLToNetStandardVersionMapping[profile];
                    tf += netstandardVersion;
                }
            }
            else
            {
                if (tfv == "")
                {
                    throw new InvalidOperationException($"{MSBuildFacts.LegacyTargetFrameworkVersionNodeName} is not set!");
                }

                tf += tfv.TrimStart('v');
            }

            return tf;
        }

        private static string GetTargetFrameworkFromProjectJson(IProject project)
        {
            var projectFolder = project.GetPropertyValue("MSBuildProjectDirectory");
            var projectJsonPath = Path.Combine(projectFolder, "project.json");

            var projectJsonContents = File.ReadAllText(projectJsonPath);

            var json = JObject.Parse(projectJsonContents);

            var frameworks = json["frameworks"];
            if (frameworks is null)
            {
                throw new InvalidOperationException($"No target framework set in this 'project.json' file located at '{projectJsonPath}'.");
            }

            var tf = ((JProperty)frameworks.Single()).Name;

            return tf;
        }
    }
}
