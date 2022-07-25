// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.WCFUpdater
{
    public class Constants
    {

        public const string TemplateUsing =
@"using CoreWCF;
using CoreWCF.Configuration;
using CoreWCF.Description;
using CoreWCF.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
";

        public const string Template =
            @"public static void Main()
{
            var builder = WebApplication.CreateBuilder();

            // Set up port (previously this was done in configuration,
            // but CoreWCF requires it be done in code)
[Port PlaceHolder]

             // Add CoreWCF services to the ASP.NET Core app's DI container
            builder.Services.AddServiceModelServices()
                            .AddServiceModelConfigurationManagerFile(""wcf.config"")
                            [Metadata1 PlaceHolder]

            var app = builder.Build();

            [Metadata2 PlaceHolder]

            // Configure CoreWCF endpoints in the ASP.NET Core hosts
            app.UseServiceModel(serviceBuilder =>
            {
                serviceBuilder.ConfigureServiceHostBase<ServiceType>(varName =>
                {
                    int UA_placeHolder;
                });
                
                serviceBuilder.AddService<ServiceType>(serviceOptions => { 
                    [ServiceDebug PlaceHolder]
                });
                
            });
            
            app.start();
            app.stop();
}";

        public const string Metadata1 = ".AddServiceModelMetadata();";
        public const string Metadata2Http = @"// Enable getting metadata/wsdl
            var serviceMetadataBehavior = app.Services.GetRequiredService<ServiceMetadataBehavior>();
            serviceMetadataBehavior.HttpGetEnabled = true;
                                   .HttpGetUrl = new Uri(""httpAddress"");";

        public const string Metadata2Https = @"// Enable getting metadata/wsdl
            var serviceMetadataBehavior = app.Services.GetRequiredService<ServiceMetadataBehavior>();
            serviceMetadataBehavior.HttpsGetEnabled = true;
                                   .HttpsGetUrl = new Uri(""httpsAddress"");";

        public const string Metadata2Both = @"// Enable getting metadata/wsdl
            var serviceMetadataBehavior = app.Services.GetRequiredService<ServiceMetadataBehavior>();
            serviceMetadataBehavior.HttpGetEnabled = true;
                                   .HttpGetUrl = new Uri(""httpAddress"");
                                   .HttpsGetEnabled = true;
                                   .HttpsGetUrl = new Uri(""httpsAddress"");";

        public const string Debug = @"serviceOptions.includeExceptionDetailInFaults = true;";

        public const string NetTcp = "            builder.WebHost.UseNetTcp(netTcpPortNum);\n";
        public const string ConfigureKestrel =
            @"            builder.WebHost.ConfigureKestrel(options =>
            {
                [Http Port]
                [Https Delegate]
            }); ";

        public const string HttpPort = "options.ListenAnyIP(httpPortNum);";
        public const string HttpsDelegate = @"options.ListenAnyIP(HttpsListenPort, listenOptions =>
                    {
                            listenOptions.UseHttps(httpsPortNum);
                    })";

        public const string CoreWCFPackages = @"<ItemGroup>
	<PackageReference Include=""CoreWCF.NetTcp"" Version=""1.1.0"" />
	<PackageReference Include = ""CoreWCF.Primitives"" Version=""1.1.0"" />
	<PackageReference Include = ""CoreWCF.ConfigurationManager"" Version=""1.1.0"" />
    <PackageReference Include = ""CoreWCF.Http"" Version=""1.1.0"" />
    <PackageReference Include = ""CoreWCF.WebHttp"" Version=""1.1.0"" />
    </ItemGroup>";

        public const string HostComment = "The host element is not supported in configuration in CoreWCF. The port that endpoints listen on is instead configured in the source code.";
        public const string BehaviorComment = "The behavior element is not supported in configuration in CoreWCF. Some service behaviors, such as metadata, are configured in the source code.";
        public const string ServiceModelComment = "Service Model elements are moved to a separate Wcf.config file located at the same directory as this file.";
        public const string MexEndpoint = "The mex endpoint is removed because it's not support in CoreWCF. Instead, the metadata service is enabled in the source code.";
    }
}
