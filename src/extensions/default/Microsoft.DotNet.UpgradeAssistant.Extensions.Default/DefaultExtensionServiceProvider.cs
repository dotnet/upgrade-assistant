using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default
{
    public class DefaultExtensionServiceProvider : IUpgradeAssistantExtensionServiceProvider
    {
        public IServiceCollection AddServices(IServiceCollection services, IConfiguration serviceConfiguration)
        {
            // TODO
            return services;
        }
    }
}
