// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Security;
using Microsoft.DotNet.PlatformAbstractions;
using Microsoft.Win32;

namespace Microsoft.DotNet.UpgradeAssistant.Telemetry
{
    internal class DockerContainerDetectorForTelemetry : IDockerContainerDetector
    {
        public IsDockerContainerResult IsDockerContainer()
        {
            switch (RuntimeEnvironment.OperatingSystemPlatform)
            {
                case Platform.Windows:
                    try
                    {
                        using (var subkey = Registry.LocalMachine.OpenSubKey("System\\CurrentControlSet\\Control"))
                        {
                            return subkey?.GetValue("ContainerType") != null
                                ? IsDockerContainerResult.True
                                : IsDockerContainerResult.False;
                        }
                    }
                    catch (SecurityException)
                    {
                        return IsDockerContainerResult.Unknown;
                    }

                case Platform.Linux:
                    return ReadProcToDetectDockerInLinux()
                        ? IsDockerContainerResult.True
                        : IsDockerContainerResult.False;
                case Platform.Unknown:
                    return IsDockerContainerResult.Unknown;
                case Platform.Darwin:
                default:
                    return IsDockerContainerResult.False;
            }
        }

        private static bool ReadProcToDetectDockerInLinux()
        {
            return IsRunningInDockerContainer || File.ReadAllText("/proc/1/cgroup").Contains("/docker/");
        }

        private static bool IsRunningInDockerContainer => EnvironmentHelper.GetEnvironmentVariableAsBool("DOTNET_RUNNING_IN_CONTAINER", false);
    }
}
