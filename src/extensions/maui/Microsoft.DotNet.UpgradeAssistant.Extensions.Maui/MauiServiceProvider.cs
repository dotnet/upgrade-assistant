using System;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Maui
{
    public class MauiServiceProvider : IExtensionServiceProvider
    {
        public void AddServices(IExtensionServiceCollection services)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }
        }
    }
}
