// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.WCFUpdater
{
    public class UpdaterFactory
    {
        public static PackageUpdater GetPackageUpdater(string path, ILogger<PackageUpdater> logger)
        {
            XDocument doc = XDocument.Load(path);
            PackageUpdater packageUpdater = new PackageUpdater(doc, logger);
            return packageUpdater;
        }

        public static ConfigUpdater GetConfigUpdater(string path, ILogger<ConfigUpdater> logger)
        {
            XDocument doc = XDocument.Load(path);
            ConfigUpdater configUpdater = new ConfigUpdater(doc, logger);
            return configUpdater;
        }

        public static SourceCodeUpdater GetSourceCodeUpdater(string path, ConfigContext context, ILogger<SourceCodeUpdater> logger)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(File.ReadAllText(path));
            SourceCodeUpdater sourceCodeUpdater = new SourceCodeUpdater(tree, UpdateTemplateCode(context, logger), logger);
            return sourceCodeUpdater;
        }

        public static List<SourceCodeUpdater> GetDirectiveUpdaters(IEnumerable<string> path, ILogger<SourceCodeUpdater> logger)
        {
            var result = new List<SourceCodeUpdater>();
            foreach (var p in path)
            {
                result.Add(new SourceCodeUpdater(CSharpSyntaxTree.ParseText(File.ReadAllText(p)), logger));
            }

            return result;
        }

        public static string UpdateTemplateCode(ConfigContext context, ILogger logger)
        {
            string template = Constants.Template;
            template = UpdatePortNumber(template, GetAllAddress(context), GetHttpsCredentials(context));
            template = UpdateServiceMetadata(template, context);
            template = template.Replace("[ServiceBuilder PlaceHolder]", AddMultipleServices(context));
            template = UpdateDIContainer(template, context);
            return template;
        }

        private static string UpdatePortNumber(string template, HashSet<Uri> port, Dictionary<Uri, Dictionary<string, string>> credentials)
        {
            HashSet<Uri> netTcp = new HashSet<Uri>(from p in port where p.Scheme == Uri.UriSchemeNetTcp select p);
            HashSet<Uri> http = new HashSet<Uri>(from p in port where p.Scheme == Uri.UriSchemeHttp select p);
            HashSet<Uri> https = new HashSet<Uri>(from p in port where p.Scheme == Uri.UriSchemeHttps select p);

            // creates the template code for host based on the scheme type
            string host = string.Empty;
            foreach (var address in netTcp)
            {
                host += Constants.NetTcp.Replace("netTcpPortNum", address.Port.ToString()) + System.Environment.NewLine;
            }

            if (http.Count > 0 || https.Count > 0)
            {
                host += Constants.ConfigureKestrel;
                var httpPort = string.Empty;
                var httpsPort = string.Empty;
                if (http.Count > 0)
                {
                    foreach (var address in http)
                    {
                        httpPort += Constants.HttpPort.Replace("httpPortNum", address.Port.ToString());
                        if (address != http.Last())
                        {
                            httpPort += System.Environment.NewLine;
                        }
                    }

                    host = host.Replace("[Http Port]",  httpPort);
                }
                else
                {
                    host = host.Replace("[Http Port]", string.Empty);
                }

                if (https.Count > 0)
                {
                    foreach (var address in https)
                    {
                        var httpsDelegate = Constants.HttpsDelegate.Replace("httpsPortNum", address.Port.ToString());
                        if (credentials.ContainsKey(address))
                        {
                            var httpsWithCert = Constants.HttpsCert.Replace("storeLocation", credentials[address]["serviceCertificate/storeLocation"])
                                       .Replace("storeName", credentials[address]["serviceCertificate/storeName"])
                                       .Replace("findType", credentials[address]["serviceCertificate/x509FindType"])
                                       .Replace("findValue", "\"" + credentials[address]["serviceCertificate/findValue"] + "\"");
                            httpsPort += httpsDelegate.Replace("[Configure Https]", httpsWithCert);
                        }
                        else
                        {
                            httpsPort += httpsDelegate.Replace("[Configure Https]", Constants.UseHttps);
                        }

                        if (address != https.Last())
                        {
                            httpsPort += System.Environment.NewLine;
                        }
                    }

                    host = host.Replace("[Https Delegate]", httpsPort);
                }
                else
                {
                    host = host.Replace("[Https Delegate]", string.Empty);
                }
            }

            return template.Replace("[Port PlaceHolder]", host);
        }

        private static HashSet<Uri> GetAllAddress(ConfigContext context)
        {
            // unions all uri from different services
            var port = new HashSet<Uri>();
            foreach (var key in context.ServiceContext.Keys)
            {
                var dic = context.ServiceContext[key].SchemeToAddressMapping;
                port.UnionWith(dic.Values);
            }

            return port;
        }

        // for each https binding that has a service cretificate configured, returns the pair of uri and credentials configuration
        private static Dictionary<Uri, Dictionary<string, string>> GetHttpsCredentials(ConfigContext context)
        {
            var credentials = new Dictionary<Uri, Dictionary<string, string>>();
            foreach (var key in context.ServiceContext.Keys)
            {
                var serviceContext = context.ServiceContext[key];
                var uri = serviceContext.SchemeToAddressMapping;
                if (serviceContext.UsesServiceCertificate && uri.ContainsKey(Uri.UriSchemeHttps))
                {
                    credentials.Add(uri[Uri.UriSchemeHttps], serviceContext.ServiceCredentialsProperties);
                }
            }

            return credentials;
        }

        private static string UpdateServiceMetadata(string template, ConfigContext context)
        {
            var hasMetadata = false;
            var metadataHttp = string.Empty;
            var metadataHttps = string.Empty;

            // updates metadata
            foreach (var key in context.ServiceContext.Keys)
            {
                var serviceContext = context.ServiceContext[key];
                var metadataType = serviceContext.MetadataType;
                var uri = serviceContext.SchemeToAddressMapping;
                if (metadataType != 0)
                {
                    hasMetadata = true;
                    if (metadataType == MetadataType.Http || metadataType == MetadataType.Both)
                    {
                        if (metadataHttp.Equals(string.Empty))
                        {
                            metadataHttp += Constants.HttpGetEnabled + System.Environment.NewLine;
                        }

                        metadataHttp += Constants.HttpGetUrl.Replace("httpAddress", new Uri(uri[Uri.UriSchemeHttp], "metadata").ToString())
                            + System.Environment.NewLine;
                    }

                    if (metadataType == MetadataType.Https || metadataType == MetadataType.Both)
                    {
                        if (metadataHttps.Equals(string.Empty))
                        {
                            metadataHttps += Constants.HttpsGetEnabled + System.Environment.NewLine;
                        }

                        metadataHttps += Constants.HttpsGetUrl.Replace("httpsAddress", new Uri(uri[Uri.UriSchemeHttps], "metadata").ToString())
                            + System.Environment.NewLine;
                    }
                }
            }

            if (!hasMetadata)
            {
                template = template.Replace("[Metadata2 PlaceHolder]", string.Empty);
            }
            else
            {
                template = template.Replace("[Metadata2 PlaceHolder]", Constants.Metadata2 + System.Environment.NewLine + metadataHttp + metadataHttps);
            }

            return template;
        }

        private static string AddMultipleServices(ConfigContext context)
        {
            var builder = string.Empty;
            foreach (var serviceName in context.ServiceContext.Keys)
            {
                var serviceContext = context.ServiceContext[serviceName];
                var addDebug = UpdateServiceDebug(Constants.AddConfigureService.Replace("ServiceType", serviceName), serviceContext.ServiceDebug);
                var addCredentials = ConfigureServiceCredentials(addDebug, context.NetTcpUsesCertificate, serviceContext.ServiceCredentialsProperties);
                builder += addCredentials + System.Environment.NewLine;
                if (serviceName != context.ServiceContext.Keys.Last())
                {
                    builder += System.Environment.NewLine;
                }
            }

            return builder;
        }

        private static string UpdateServiceDebug(string template, Dictionary<string, string> debug)
        {
            // updates service debug
            var result = string.Empty;
            if (debug.ContainsKey("httpHelpPageEnabled") && debug["httpHelpPageEnabled"].Equals("false", StringComparison.OrdinalIgnoreCase))
            {
                result += Constants.Trivia + Constants.HttpPageEnabled + System.Environment.NewLine;
            }

            if (debug.ContainsKey("httpsHelpPageEnabled") && debug["httpsHelpPageEnabled"].Equals("false", StringComparison.OrdinalIgnoreCase))
            {
                result += Constants.Trivia + Constants.HttpsPageEnabled + System.Environment.NewLine;
            }

            if (debug.ContainsKey("httpHelpPageUrl"))
            {
                result += Constants.Trivia + Constants.HttpPageUrl.Replace("address", debug["httpHelpPageUrl"]) + System.Environment.NewLine;
            }

            if (debug.ContainsKey("httpsHelpPageUrl"))
            {
                result += Constants.Trivia + Constants.HttpsPageUrl.Replace("address", debug["httpsHelpPageUrl"]) + System.Environment.NewLine;
            }

            if (debug.ContainsKey("includeExceptionDetailInFaults") && debug["includeExceptionDetailInFaults"].Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                result += Constants.Trivia + Constants.DebugFaults;
            }

            if (result.EndsWith(System.Environment.NewLine))
            {
                result = result.TrimEnd(System.Environment.NewLine.ToCharArray());
            }

            return template.Replace("[ServiceDebug PlaceHolder]", result);
        }

        // update template to add metadata service, service types, and windows authentication to the DI container
        private static string UpdateDIContainer(string template, ConfigContext context)
        {
            var result = string.Empty;
            if (context.HasMetadata)
            {
                result += Constants.Metadata1 + System.Environment.NewLine;
            }

            result += AddServiceType(context);

            if (context.HasWindowsAuthentication)
            {
                result += System.Environment.NewLine + Constants.HttpWindowsAuth;
            }

            result += ";";
            return template.Replace("[Add to DI Container]", result);
        }

        private static string AddServiceType(ConfigContext context)
        {
            var result = string.Empty;
            foreach (var serviceType in context.ServiceContext.Keys)
            {
                result += Constants.ServiceType.Replace("ServiceType", serviceType);
                if (serviceType != context.ServiceContext.Keys.Last())
                {
                    result += System.Environment.NewLine;
                }
            }

            return result;
        }

        private static string ConfigureServiceCredentials(string template, bool hasNetTcpCert, Dictionary<string, string> credentials)
        {
            var cert = string.Empty;

            // add netTcp service certificate if applicable
            if (hasNetTcpCert)
            {
                string service = Constants.NetTcpCert.Replace("storeLocation", credentials["serviceCertificate/storeLocation"])
                                                     .Replace("storeName", credentials["serviceCertificate/storeName"])
                                                     .Replace("findType", credentials["serviceCertificate/x509FindType"])
                                                     .Replace("findValue", "\"" + credentials["serviceCertificate/findValue"] + "\"");
                cert = Constants.Trivia + service + System.Environment.NewLine;
            }

            // configure client certificate
            if (credentials.ContainsKey("clientCertificate/findValue"))
            {
                string client = Constants.ClientCert.Replace("storeLocation", credentials["clientCertificate/storeLocation"])
                                                    .Replace("storeName", credentials["clientCertificate/storeName"])
                                                    .Replace("findType", credentials["clientCertificate/x509FindType"])
                                                    .Replace("findValue", "\"" + credentials["clientCertificate/findValue"] + "\"");
                cert += Constants.Trivia + client + System.Environment.NewLine;
            }

            // configure client certificate authentication
            var auth = new List<string[]>();
            if (credentials.ContainsKey("clientCertificate/certificateValidationMode"))
            {
                cert += Constants.Trivia + Constants.ClientAuthMode.Replace("ModeType", credentials["clientCertificate/certificateValidationMode"]) + System.Environment.NewLine;
                if (credentials["clientCertificate/certificateValidationMode"].Equals("custom", StringComparison.OrdinalIgnoreCase))
                {
                    cert += Constants.Trivia + Constants.ClientAuthCustom.Replace("CustomValidatorType", credentials["clientCertificate/customCertificateValidatorType"]) + System.Environment.NewLine;
                }
            }

            // configure username authentication
            if (credentials.ContainsKey("userNameAuthentication/userNamePasswordValidationMode"))
            {
                cert += Constants.Trivia + Constants.UserAuthMode.Replace("ModeType", credentials["userNameAuthentication/userNamePasswordValidationMode"]) + System.Environment.NewLine;
                if (credentials["userNameAuthentication/userNamePasswordValidationMode"].Equals("custom", StringComparison.OrdinalIgnoreCase))
                {
                    cert += Constants.Trivia + Constants.UserAuthCustom.Replace("CustomValidatorType", credentials["userNameAuthentication/customUserNamePasswordValidatorType"]) + System.Environment.NewLine;
                }
            }

            // configure windows group
            if (credentials.ContainsKey("windowsAuthentication/includeWindowsGroups"))
            {
                cert += Constants.Trivia + Constants.WindowsAuth.Replace("boolean", credentials["windowsAuthentication/includeWindowsGroups"]);
            }

            return template.Replace("[ServiceCredentials PlaceHolder]", cert);
        }
    }
}
