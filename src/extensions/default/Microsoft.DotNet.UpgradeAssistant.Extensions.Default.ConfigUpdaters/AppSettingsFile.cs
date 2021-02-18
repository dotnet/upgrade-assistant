using Microsoft.Extensions.Configuration;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.ConfigUpdaters
{
    public class AppSettingsFile
    {
        public AppSettingsFile(string path)
        {
            FilePath = path;
            Configuration = new ConfigurationBuilder()
                .AddJsonFile(path)
                .Build();
        }

        public string FilePath { get; }

        public IConfiguration Configuration { get; }
    }
}
