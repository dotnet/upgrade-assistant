// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.WCFUpdater
{
    public static class WCFUpdateChecker
    {
        public static bool IsWCFUpdateApplicable(Dictionary<string, IEnumerable<string>> path, ILogger logger)
        {
            try
            {
                var config = IsConfigApplicable(XDocument.Load(path["config"].First()));
                var source = IsSourceCodeApplicable(CSharpSyntaxTree.ParseText(File.ReadAllText(path["main"].First())));
                var proj = IsProjFileApplicable(XDocument.Load(path["proj"].First()));

                if (config)
                {
                    logger.LogInformation(path["config"].First(), $"This config file is applicable for upgrade: {path}. System.serviceModel/services elements were found.");
                }
                else
                {
                    logger.LogInformation(path["config"].First(), $"This config file is not applicable for update: {path}. System.serviceModel/services elements were not found.");
                }

                if (source)
                {
                    logger.LogInformation(path["main"].First(), $"This  file is applicable for upgrade: {path}. ServiceHost object was found.");
                }
                else
                {
                    logger.LogInformation(path["main"].First(), $"This config file is not applicable for update: {path}. ServiceHost object was not found.");
                }

                if (proj)
                {
                    logger.LogInformation(path["proj"].First(), $"This project file is applicable for upgrade: {path}. Reference to System.serviceModel was found.");
                }
                else
                {
                    logger.LogInformation(path["proj"].First(), $"This project file is not applicable for upgrade: {path}. Reference to System.serviceModel was not found.");
                }

                return config && source && proj;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Unexpected error happened when checking if this project if applicable for WCF update.");
                return false;
            }
        }

        public static bool IsConfigApplicable(XDocument config)
        {
            var serviceModel = config.Root.DescendantsAndSelf("system.serviceModel").First();
            foreach (var element in serviceModel.Descendants())
            {
                if (element.Name == "services" && element.HasElements)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsSourceCodeApplicable(SyntaxTree tree)
        {
            var descendants = from id in tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>()
                                where id.Identifier.ValueText.Equals("ServiceHost", StringComparison.Ordinal)
                                select id;
            return descendants.Any();
        }

        public static bool IsProjFileApplicable(XDocument doc)
        {
            foreach (var element in doc.Root.Descendants("PackageReference"))
            {
                if (element.Attribute("Include").Value.IndexOf("System.ServiceModel", StringComparison.Ordinal) >= 0)
                {
                    return true;
                }
            }

            if (doc.Root.Attribute("Sdk") is not null && doc.Root.Attribute("Sdk").Value.IndexOf("Web", StringComparison.Ordinal) < 0)
            {
                return true;
            }

            return false;
        }
    }
}
