﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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

        public static SourceCodeUpdater GetSourceCodeUpdater(string path, Dictionary<string, object> context, ILogger<SourceCodeUpdater> logger)
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

        private static string UpdateTemplateCode(Dictionary<string, object> context, ILogger logger)
        {
            string template = Constants.Template;
            Dictionary<string, Uri> uri = (Dictionary<string, Uri>)context["uri"];
            template = UpdatePortNumber(template, uri, (HashSet<string>)context["bindings"]);
            template = UpdateServiceBehavior(template, (MetadataType)context["metadata"], (bool)context["debug"], uri);
            return template;
        }

        private static string UpdatePortNumber(string template, Dictionary<string, Uri> portNum, HashSet<string> bindings)
        {
            bool httpBinding = false, httpsBinding = false;
            foreach (string b in bindings)
            {
                if (b.IndexOf("HttpBinding", StringComparison.Ordinal) >= 0)
                {
                    httpBinding = true;
                }
                else if (b.IndexOf("HttpsBinding", StringComparison.Ordinal) >= 0)
                {
                    httpsBinding = true;
                }
            }

            // adds default address if binding exists
            if (!portNum.ContainsKey(Uri.UriSchemeNetTcp) && bindings.Contains("NetTcpBinding"))
            {
                portNum.Add(Uri.UriSchemeNetTcp, new Uri("http://localhost:808"));
            }

            if (!portNum.ContainsKey(Uri.UriSchemeHttp) && httpBinding)
            {
                portNum.Add(Uri.UriSchemeHttp, new Uri("http://localhost:80"));
            }

            if (!portNum.ContainsKey(Uri.UriSchemeHttps) && httpsBinding)
            {
                portNum.Add(Uri.UriSchemeHttps, new Uri("http://localhost:443"));
            }

            // creates the template code for host based on the scheme type
            string host = string.Empty;
            if (portNum.ContainsKey(Uri.UriSchemeNetTcp))
            {
                host += Constants.NetTcp + System.Environment.NewLine;
                host = host.Replace("netTcpPortNum", portNum[Uri.UriSchemeNetTcp].Port.ToString());
            }

            if (portNum.ContainsKey(Uri.UriSchemeHttp) || portNum.ContainsKey(Uri.UriSchemeHttps))
            {
                host += Constants.ConfigureKestrel;
                if (portNum.ContainsKey(Uri.UriSchemeHttp))
                {
                    host = host.Replace("[Http Port]", Constants.HttpPort);
                    host = host.Replace("httpPortNum", portNum[Uri.UriSchemeHttp].Port.ToString());
                }
                else
                {
                    host = host.Replace("[Http Port]", string.Empty);
                }

                if (portNum.ContainsKey(Uri.UriSchemeHttps))
                {
                    host = host.Replace("[Https Delegate]", Constants.HttpsDelegate);
                    host = host.Replace("httpsPortNum", portNum[Uri.UriSchemeHttps].Port.ToString());
                }
                else
                {
                    host = host.Replace("[Https Delegate]", string.Empty);
                }
            }

            return template.Replace("[Port PlaceHolder]", host);
        }

        private static string UpdateServiceBehavior(string template, MetadataType metadataType, bool debug, Dictionary<string, Uri> uri)
        {
            // updates metadata
            if (metadataType != MetadataType.None)
            {
                template = template.Replace("[Metadata1 PlaceHolder]", Constants.Metadata1);
                if (metadataType == MetadataType.Http)
                {
                    string metadata2 = Constants.Metadata2Http.Replace("httpAddress", new Uri(uri[Uri.UriSchemeHttp], "metadata").ToString());
                    template = template.Replace("[Metadata2 PlaceHolder]", metadata2);
                }
                else if (metadataType == MetadataType.Https)
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
