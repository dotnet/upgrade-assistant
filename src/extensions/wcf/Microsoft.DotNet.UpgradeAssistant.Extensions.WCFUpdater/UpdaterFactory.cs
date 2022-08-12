// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.WCFUpdater
{
    public class UpdaterFactory
    {
        public static PackageUpdater? GetPackageUpdater(string path, ILogger logger)
        {
            try
            {
                XDocument doc = XDocument.Load(path);
                PackageUpdater packageUpdater = new PackageUpdater(doc, logger);
                return packageUpdater;
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Error loading project file {path}");
                return null;
            }
        }

        public static ConfigUpdater? GetConfigUpdater(string path, ILogger logger)
        {
            try
            {
                XDocument doc = XDocument.Load(path);
                ConfigUpdater configUpdater = new ConfigUpdater(doc, logger);
                return configUpdater;
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Error loading project file {path}");
                return null;
            }
        }

        public static SourceCodeUpdater? GetSourceCodeUpdater(string path, Dictionary<string, Dictionary<string, object>> context, ILogger logger)
        {
            try
            {
                SyntaxTree tree = CSharpSyntaxTree.ParseText(File.ReadAllText(path));
                SourceCodeUpdater sourceCodeUpdater = new SourceCodeUpdater(tree, UpdateTemplateCode(context, logger), logger);
                return sourceCodeUpdater;
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Error parsing project file {path}");
                return null;
            }
        }

        public static List<SourceCodeUpdater>? GetDirectiveUpdaters(IEnumerable<string> path, ILogger logger)
        {
            try
            {
                var result = new List<SourceCodeUpdater>();
                foreach (var p in path)
                {
                    result.Add(new SourceCodeUpdater(CSharpSyntaxTree.ParseText(File.ReadAllText(p)), logger));
                }

                return result;
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Error parsing project file {path}");
                return null;
            }
        }

        private static string UpdateTemplateCode(Dictionary<string, Dictionary<string, object>> context, ILogger logger)
        {
            string template = Constants.Template;
            template = UpdatePortNumber(template, GetAllAddress(context, logger));
            //template = UpdateServiceMetadata(template, (int)context["metadata"], uri);
            template = template.Replace("[ServiceBuilderPlaceHolder]", AddMultipleServices(context, logger));
            return template;
        }

        private static string UpdatePortNumber(string template, HashSet<Uri> port)
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
                if (http.Count > 0)
                {
                    foreach (var address in http)
                    {
                        host += Constants.HttpPort.Replace("httpPortNum", address.Port.ToString()) + System.Environment.NewLine;
                    }
                }
                else
                {
                    host = host.Replace("[Http Port]", string.Empty);
                }

                if (https.Count > 0)
                {
                    foreach (var address in https)
                    {
                        host += Constants.HttpsDelegate.Replace("httpsPortNum", address.Port.ToString()) + System.Environment.NewLine;
                    }
                }
                else
                {
                    host = host.Replace("[Https Delegate]", string.Empty);
                }
            }

            return template.Replace("[Port PlaceHolder]", host);
        }

        private static HashSet<Uri> GetAllAddress(Dictionary<string, Dictionary<string, object>> context, ILogger logger)
        {
            // unions all uri from different services
            HashSet<Uri> port = new HashSet<Uri>();
            foreach (var key in context.Keys)
            {
                port.UnionWith((HashSet<Uri>)context[key]["uri"]);
            }

            return port;
        }

        private static string UpdateServiceMetadata(string template, int metadataType, Dictionary<string, Uri> uri)
        {
            // updates metadata
            if (metadataType != 0)
            {
                template = template.Replace("[Metadata1 PlaceHolder]", Constants.Metadata1);
                if (metadataType == 1)
                {
                    string metadata2 = Constants.Metadata2Http.Replace("httpAddress", new Uri(uri[Uri.UriSchemeHttp], "metadata").ToString());
                    template = template.Replace("[Metadata2 PlaceHolder]", metadata2);
                }
                else if (metadataType == 2)
                {
                    string metadata2 = Constants.Metadata2Https.Replace("httpsAddress", new Uri(uri[Uri.UriSchemeHttps], "metadata").ToString());
                    template = template.Replace("[Metadata2 PlaceHolder]", metadata2);
                }
                else
                {
                    string metadata2 = Constants.Metadata2Both.Replace("httpAddress", new Uri(uri[Uri.UriSchemeHttp], "metadata").ToString());
                    metadata2 = metadata2.Replace("httpsAddress", Path.Combine(uri[Uri.UriSchemeHttps].ToString(), "metadata"));
                    template = template.Replace("[Metadata2 PlaceHolder]", metadata2);
                }
            }
            else
            {
                template = template.Replace("[Metadata1 PlaceHolder]", string.Empty);
                template = template.Replace("[Metadata2 PlaceHolder]", string.Empty);
            }

            return template;
        }

        private static string AddMultipleServices(Dictionary<string, Dictionary<string, object>> context, ILogger logger)
        {
            var builder = string.Empty;
            foreach (var serviceName in context.Keys)
            {
                builder += UpdateServiceDebug(Constants.AddConfigureService.Replace("ServiceType", serviceName), (bool)context[serviceName]["debug"])
                    + System.Environment.NewLine;
            }

            return builder;
        }

        private static string UpdateServiceDebug(string template, bool debug)
        {
            // updates service debug
            if (debug)
            {
                template = template.Replace("[ServiceDebug PlaceHolder]", Constants.Debug);
            }
            else
            {
                template = template.Replace("[ServiceDebug PlaceHolder]", string.Empty);
            }

            return template;
        }
    }
}
