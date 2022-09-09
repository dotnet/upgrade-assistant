// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.WCFUpdater
{
    public static class UpdateRunner
    {
        public static List<XDocument> ConfigUpdate(ConfigUpdater updater, ILogger logger)
        {
            logger.LogDebug("Start updating configuration files...");
            XDocument wcfConfig = updater.GenerateNewConfig();
            XDocument appConfig = updater.UpdateOldConfig();
            var result = new List<XDocument>
                        {
                            appConfig,
                            wcfConfig
                        };
            logger.LogInformation("Finish updating configuration files.");
            return result;
        }

        public static void WriteConfigUpdate(XDocument appConfig, XDocument wcfConfig, string path, ILogger logger)
        {
            logger.LogDebug("Start writing changes to configuration files...");
            wcfConfig.Save(Path.Combine(Path.GetDirectoryName(path), "wcf.config"));
            appConfig.Save(path);
            logger.LogInformation("Finish writing changes to configuration files.");
        }

        public static XDocument PackageUpdate(PackageUpdater packageUpdater, ILogger logger)
        {
            logger.LogDebug("Start updating project file...");
            packageUpdater.UpdatePackages();
            var projFile = packageUpdater.UpdateSDK();
            logger.LogInformation("Finish updating project file.");
            return projFile;
        }

        public static void WritePackageUpdate(XDocument projFile, string path, ILogger logger)
        {
            logger.LogDebug("Start writing changes to project file...");
            projFile.Save(path);
            logger.LogInformation("Finish writing changes to project file.");
        }

        public static SyntaxNode SourceCodeUpdate(SourceCodeUpdater sourceCodeUpdater, ILogger logger)
        {
            logger.LogDebug("Start updating source code...");
            var root = sourceCodeUpdater.SourceCodeUpdate();
            logger.LogInformation("Finish updating source code.");
            return root;
        }

        public static void WriteSourceCodeUpdate(SyntaxNode root, string path, ILogger logger)
        {
            logger.LogDebug("Start writing changes to the source code to replace the ServiceHost instance(s).");
            File.WriteAllText(path, root.ToFullString());
            logger.LogInformation("Finish writing changes to the source code to replace the ServiceHost instance(s).");
        }

        public static List<SyntaxNode> UsingDirectivesUpdate(List<SourceCodeUpdater> list, ILogger logger)
        {
            logger.LogDebug("Start updating using directives...");
            var result = new List<SyntaxNode>();
            foreach (var updater in list)
            {
                result.Add(updater.UpdateDirectives());
            }

            logger.LogInformation("Finish updating using directives.");
            return result;
        }

        public static void WriteUsingDirectivesUpdate(List<SyntaxNode> root, IEnumerable<string> path, ILogger logger)
        {
            logger.LogDebug("Start writing changes to the source code to update using directives...");
            if (root.Count != path.Count())
            {
                logger.LogError("The number of files does not match the number of path. Cannot write directive updates.");
                return;
            }

            for (int i = 0; i < root.Count; i++)
            {
                File.WriteAllText(path.ElementAtOrDefault(i), root[i].ToFullString());
            }

            logger.LogInformation("Finish writing changes to the source code to update using directives.");
        }
    }
}
