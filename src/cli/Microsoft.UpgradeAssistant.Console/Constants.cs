using System.Reflection;

namespace Microsoft.UpgradeAssistant.Cli
{
    public static class Constants
    {
        public static string Version
        {
            get
            {
                var attribute = typeof(Constants).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                return attribute?.InformationalVersion ?? "0.0.0-unspecified";
            }
        }
    }
}
