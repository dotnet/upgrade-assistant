using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    public interface IUpgradeAssistantExtensionServiceProvider
    {
        IServiceCollection AddServices(IServiceCollection services, IConfiguration serviceConfiguration);
    }
}
