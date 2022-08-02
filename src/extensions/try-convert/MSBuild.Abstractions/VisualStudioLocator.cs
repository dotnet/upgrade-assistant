using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Setup.Configuration;

namespace MSBuild.Abstractions
{
    /// <summary>
    ///     Helper class to wrap the Microsoft.VisualStudio.Setup.Configuration.Interop API to query
    ///     Visual Studio setup for instances installed on the machine.
    ///     Code derived from sample: https://code.msdn.microsoft.com/Visual-Studio-Setup-0cedd331.
    /// </summary>
    /// <remarks>
    /// The package is not supported on .NET 5, so we copied the COM declarations to use instead.
    /// </remarks>
    ///<codereference>
    ///https://github.com/dotnet/upgrade-assistant/blob/main/src/components/Microsoft.DotNet.UpgradeAssistant.MSBuild/VisualStudioFinder.cs
    ///Placeholder class till this PR is merged into MSBuildLocator : https://github.com/microsoft/MSBuildLocator/pull/132
    /// </code>

    public class VisualStudioLocator
    {
        private const int REGDB_E_CLASSNOTREG = unchecked((int)0x80040154);
        public VisualStudioLocator( )
        {
        }

        public string? GetLatestVisualStudioPath()
        {
            var latest = GetLatestPath();

            if (latest is null)
            {
                throw new InvalidOperationException("Did not find a Visual Studio instance");
            }

            var version = Version.Parse(latest.GetInstallationVersion());
            var installation = latest.GetInstallationPath();

            if (Directory.Exists(installation))
            {
                //_logger.LogDebug("Found Visual Studio {VsVersion} at {VsPath}", version, installation);

                return installation;
            }
            else
            {
                //_logger.LogInformation("Found Visual Studio {VsVersion}, but directory '{VsPath}' does not exist.", version, installation);

                return null;
            }
        }

        private static ISetupInstance2? GetLatestPath()
        {
            var result = default(ISetupInstance2);
            var resultVersion = new Version(0, 0);

            try
            {
                // This code is not obvious. See the sample (link above) for reference.
                var query = (ISetupConfiguration2)GetQuery();
                var e = query.EnumAllInstances();

                int fetched;
                var instances = new ISetupInstance[1];
                do
                {
                    // Call e.Next to query for the next instance (single item or nothing returned).
                    e.Next(1, instances, out fetched);
                    if (fetched <= 0)
                    {
                        continue;
                    }

                    var instance = (ISetupInstance2)instances[0];
                    var state = instance.GetState();

                    if (!Version.TryParse(instance.GetInstallationVersion(), out var version))
                    {
                        continue;
                    }

                    // If the install was complete and a valid version, consider it.
                    if (state == InstanceState.Complete ||
                        (state.HasFlag(InstanceState.Registered) && state.HasFlag(InstanceState.NoRebootRequired)))
                    {
                        var instanceHasMSBuild = false;

                        foreach (var package in instance.GetPackages())
                        {
                            if (string.Equals(package.GetId(), "Microsoft.Component.MSBuild", StringComparison.OrdinalIgnoreCase))
                            {
                                instanceHasMSBuild = true;
                                break;
                            }
                        }

                        if (instanceHasMSBuild && instance is not null && version > resultVersion)
                        {
                            result = instance;
                            resultVersion = version;
                        }
                    }
                }
                while (fetched > 0);
            }
            catch (COMException)
            {
            }
            catch (DllNotFoundException)
            {
                // This is OK, VS "15" or greater likely not installed.
            }

            return result;
        }

        private static ISetupConfiguration GetQuery()
        {
            try
            {
                // Try to CoCreate the class object.
                return new SetupConfiguration();
            }
            catch (COMException ex) when (ex.ErrorCode == REGDB_E_CLASSNOTREG)
            {
                // Try to get the class object using app-local call.
                //var result = GetSetupConfiguration(out var query, IntPtr.Zero);

                //if (result < 0)
                //{
                //    throw;
                //}

                //return query;

                throw new ArgumentException("Error SetupConfiguration " + ex.Message);
            }
        }
    }
}
