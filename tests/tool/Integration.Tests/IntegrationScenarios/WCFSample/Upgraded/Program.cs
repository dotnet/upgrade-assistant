using Serilog;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using CoreWCF;
using CoreWCF.Configuration;
using CoreWCF.Description;
using CoreWCF.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace BeanTraderServer
{
    class Program
    {
        static void Main()
        {
            ConfigureLogging();
            var builder = WebApplication.CreateBuilder();

            // Set up port (previously this was done in configuration,
            // but CoreWCF requires it be done in code)
            builder.WebHost.UseNetTcp(90);
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(8011);
                
            }); 

             // Add CoreWCF services to the ASP.NET Core app's DI container
            builder.Services.AddServiceModelServices()
                            .AddServiceModelConfigurationManagerFile("wcf.config")
                            .AddServiceModelMetadata();

            var app = builder.Build();

            // Enable getting metadata/wsdl
            var serviceMetadataBehavior = app.Services.GetRequiredService<ServiceMetadataBehavior>();
            serviceMetadataBehavior.HttpGetEnabled = true;
                                   .HttpGetUrl = new Uri("http://localhost:8011/");

            // Configure CoreWCF endpoints in the ASP.NET Core hosts
            app.UseServiceModel(serviceBuilder =>
            {
                serviceBuilder.ConfigureServiceHostBase<BeanTrader>(host =>
                {
                    var certPath = Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "BeanTrader.pfx");
                    host.Credentials.ServiceCertificate.Certificate = new X509Certificate2(certPath, "password");
                    host.Credentials.ClientCertificate.Authentication.CertificateValidationMode = X509CertificateValidationMode.None;
                });
                
                serviceBuilder.AddService<BeanTrader>(serviceOptions => { 
                    serviceOptions.includeExceptionDetailInFaults = true;
                });
                
            });
            
            app.start();
            Log.Information("Bean Trader Service listening");
            WaitForExitSignal();
            Log.Information("Shutting down...");
            app.stop();
        }

        private static void WaitForExitSignal()
        {
            Console.WriteLine("Press enter to exit");
            Console.ReadLine();
        }

        private static void ConfigureLogging()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            Log.Information("Logging initialized");
        }
    }
}
