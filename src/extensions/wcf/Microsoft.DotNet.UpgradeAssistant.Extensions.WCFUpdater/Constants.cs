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
using System.Threading.Tasks;
";

        public const string TemplateUsingShort =
@"using CoreWCF;
using CoreWCF.Configuration;
using CoreWCF.Description;
using CoreWCF.Security;
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
[Add to DI Container]

            var app = builder.Build();

            [Metadata2 PlaceHolder]
            // Configure CoreWCF endpoints in the ASP.NET Core hosts
            app.UseServiceModel(serviceBuilder =>
            {
[ServiceBuilder PlaceHolder]
            });
            
            await app.StartAsync();
            await app.StopAsync();
}";

        public const string Metadata1 = "                            .AddServiceModelMetadata()";
        public const string Metadata2 = @"// Enable getting metadata/wsdl
            var serviceMetadataBehavior = app.Services.GetRequiredService<ServiceMetadataBehavior>();";

        public const string HttpGetEnabled = "            serviceMetadataBehavior.HttpGetEnabled = true;";
        public const string HttpsGetEnabled = "            serviceMetadataBehavior.HttpsGetEnabled = true;";
        public const string HttpGetUrl = @"            serviceMetadataBehavior.HttpGetUrl = new Uri(""httpAddress"");";
        public const string HttpsGetUrl = @"            serviceMetadataBehavior.HttpsGetUrl = new Uri(""httpsAddress"");";

        public const string DebugFaults = "serviceOptions.DebugBehavior.IncludeExceptionDetailInFaults = true;";
        public const string HttpPageEnabled = "serviceOptions.DebugBehavior.HttpHelpPageEnabled = false;";
        public const string HttpsPageEnabled = "serviceOptions.DebugBehavior.HttpsHelpPageEnabled = false;";
        public const string HttpPageUrl = @"serviceOptions.DebugBehavior.HttpHelpPageUrl = new Uri(""address"");";
        public const string HttpsPageUrl = @"serviceOptions.DebugBehavior.HttpsHelpPageUrl = new Uri(""address"");";

        public const string NetTcp = "            builder.WebHost.UseNetTcp(netTcpPortNum);";
        public const string ConfigureKestrel =
            @"            builder.WebHost.ConfigureKestrel(options =>
            {
                [Http Port]
                [Https Delegate]
            });";

        public const string HttpPort = "options.ListenAnyIP(httpPortNum);";
        public const string HttpsDelegate = @"options.ListenAnyIP(httpsPortNum, listenOptions =>
                {
                    [Configure Https]
                });";

        public const string UseHttps = "listenOptions.UseHttps();";
        public const string CoreWCFPackages = @"<ItemGroup>
	<PackageReference Include=""CoreWCF.NetTcp"" Version=""1.1.0"" />
	<PackageReference Include = ""CoreWCF.Primitives"" Version=""1.1.0"" />
	<PackageReference Include = ""CoreWCF.ConfigurationManager"" Version=""1.1.0"" />
    <PackageReference Include = ""CoreWCF.Http"" Version=""1.1.0"" />
    <PackageReference Include = ""CoreWCF.WebHttp"" Version=""1.1.0"" />
    </ItemGroup>";

        public const string AddConfigureService =
              @"                serviceBuilder.AddService<ServiceType>(serviceOptions => 
                {
[ServiceDebug PlaceHolder]
                });

                serviceBuilder.ConfigureServiceHostBase<ServiceType>(varName =>
                {
                    int UA_placeHolder;
[ServiceCredentials PlaceHolder]
                });";

        public const string ServiceType = "                            .AddTransient<ServiceType>()";
        public const string HttpWindowsAuth = "                            .AddAuthentication(NegotiateDefaults.AuthenticationScheme).AddNegotiate()";
        public const string HostComment = "The host element is not supported in configuration in CoreWCF. The port that endpoints listen on is instead configured in the source code.";
        public const string BehaviorComment = "The behavior element is not supported in configuration in CoreWCF. Some service behaviors, such as metadata, are configured in the source code.";
        public const string ServiceModelComment = " system.serviceModel section is moved to a separate wcf.config file located at the same directory as this file.";
        public const string MexEndpoint = "The mex endpoint is removed because it's not support in CoreWCF. Instead, the metadata service is enabled in the source code.";

        // service credentials related constants
        public const string HttpsCert = @"X509Store store = new X509Store(StoreName.storeName, StoreLocation.storeLocation);
                    store.Open(OpenFlags.ReadOnly);
                    X509Certificate2Collection certs = store.Certificates.Find(X509FindType.findType, findValue, false);
                    listenOptions.UseHttps(new X509Certificate2(certs[0]));";

        public const string NetTcpCert = "host.Credentials.ServiceCertificate.SetCertificate(StoreLocation.storeLocation, StoreName.storeName, X509FindType.findType, findValue);";
        public const string ClientCert = "host.Credentials.ClientCertificate.SetCertificate(StoreLocation.storeLocation, StoreName.storeName, X509FindType.findType, findValue);";
        public const string ClientAuthMode = "host.Credentials.ClientCertificate.Authentication.CertificateValidationMode = X509CertificateValidationMode.ModeType;";
        public const string ClientAuthCustom = "host.Credentials.ClientCertificate.Authentication.CustomCertificateValidator = new CustomValidatorType();";
        public const string UserAuthMode = "host.Credentials.UserNameAuthentication.UserNamePasswordValidationMode = UserNamePasswordValidationMode.ModeType;";
        public const string UserAuthCustom = "host.Credentials.UserNameAuthentication.CustomUserNamePasswordValidator = new CustomValidatorType();";
        public const string WindowsAuth = "host.Credentials.WindowsAuthentication.IncludeWindowsGroups = boolean;";
        public const string Trivia = "                    ";
    }
}
