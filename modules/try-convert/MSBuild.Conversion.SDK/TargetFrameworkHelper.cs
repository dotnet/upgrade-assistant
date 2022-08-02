using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

using MSBuild.Conversion.Facts;
using NuGet.Versioning;

namespace MSBuild.Conversion.SDK
{
    public static class TargetFrameworkHelper
    {
        /// <summary>
        /// Determine the TFM to use based on what is installed on the users machine
        /// </summary>
        public static string FindHighestInstalledTargetFramework(bool usePreviewSDK, string msbuildPath)
        {
            // Finds SDK path
            string? sdkPath = null;
            try
            {
                if (string.IsNullOrWhiteSpace(msbuildPath))
                {
                    throw new InvalidOperationException("msbuildPath is rquired");
                }

                sdkPath = Path.GetFullPath(Path.Combine(msbuildPath, "..", ".."));
            }
            catch (Exception)
            {
                Console.WriteLine("Unable to find the .NET SDK on this machine, manually pass '-tfm'");
                throw;
            }

            try
            {
                // Find templates path
                var templatesPath = Path.Combine(sdkPath, "templates");

                // Find highest SDK path (should include previews?)
                var largestVersion = NuGetVersion.Parse("0.0.0.0");
                var templatePath = string.Empty;
                foreach (var templateDirectory in Directory.EnumerateDirectories(templatesPath))
                {
                    if (NuGetVersion.TryParse(Path.GetFileName(templateDirectory), out var templatesVersion) &&
                        templatesVersion > largestVersion)
                    {
                        if (usePreviewSDK)
                        {
                            largestVersion = templatesVersion;
                            templatePath = Path.GetFullPath(templateDirectory);
                        }
                        else if (!templatesVersion.IsPrerelease)
                        {
                            largestVersion = templatesVersion;
                            templatePath = Path.GetFullPath(templateDirectory);
                        }
                    }
                }

                var templateFiles = Directory.EnumerateFiles(templatePath, "microsoft.dotnet.common.projecttemplates.*.nupkg", SearchOption.TopDirectoryOnly);
                // get the highest version of the files found, based on NuGetVersion
                var templateNugetPackagePath = templateFiles.OrderByDescending(p =>
                {
                    var versionStr = Path.GetFileNameWithoutExtension(p).Substring("microsoft.dotnet.common.projecttemplates.".Length);
                    //first two numbers in the version are part of the name of the package, not its nuget version.
                    versionStr = string.Join(".", versionStr.Split(".").Skip(2));
                    var version = NuGetVersion.Parse(versionStr);
                    return (!usePreviewSDK && version.IsPrerelease) ? new NuGetVersion(0, 0, 0) : version;
                }).First();
                // upzip the common project templates into memory
                using var templateNugetPackageFile = File.OpenRead(templateNugetPackagePath);
                using var templateNugetPackage = new ZipArchive(templateNugetPackageFile, ZipArchiveMode.Read);
                var templatesJsonFile = templateNugetPackage.Entries
                    .Where(x => x.Name.Equals("template.json", StringComparison.OrdinalIgnoreCase) &&
                                x.FullName.Contains("ClassLibrary-CSharp", StringComparison.OrdinalIgnoreCase)).Single();
                using var templatesJson = templatesJsonFile.Open();

                // read the template.json file to see what the tfm is called
                var doc = JsonDocument.ParseAsync(templatesJson).GetAwaiter().GetResult();

                var tfm = doc.RootElement.GetProperty("baselines").GetProperty("app").GetProperty("defaultOverrides").GetProperty("Framework").GetString();
                if (string.IsNullOrEmpty(tfm))
                {
                    return MSBuildFacts.Net5;
                }
                else
                {
                    return tfm;
                }
            }
            catch (Exception)
            {
                return MSBuildFacts.NetCoreApp31;
            }

        }

        /// <summary>
        /// Reject obviously wrong TFM specifiers 
        /// </summary>
        public static bool IsValidTargetFramework(string tfm)
            => !tfm.Contains(" ") && tfm.Contains("net") && Regex.Match(tfm, "[0-9]").Success;
    }
}
