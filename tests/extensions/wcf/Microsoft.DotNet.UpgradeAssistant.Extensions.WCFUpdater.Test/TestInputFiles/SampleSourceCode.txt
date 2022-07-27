using Serilog;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Security;

namespace BeanTraderServer
{
    class Program
    {
        static void Main()
        {
            ConfigureLogging();

            using (var host = new ServiceHost(typeof(BeanTrader)))
            {
                // For demo purposes, just load the key from disk so that no one needs to install an untrustworthy self-signed cert
                var certPath = Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "BeanTrader.pfx");
                host.Credentials.ServiceCertificate.Certificate = new X509Certificate2(certPath, "password");
                host.Credentials.ClientCertificate.Authentication.CertificateValidationMode = X509CertificateValidationMode.None;
                host.Open();
                Log.Information("Bean Trader Service listening");
                WaitForExitSignal();
                Log.Information("Shutting down...");
                host.Close();
            }
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
