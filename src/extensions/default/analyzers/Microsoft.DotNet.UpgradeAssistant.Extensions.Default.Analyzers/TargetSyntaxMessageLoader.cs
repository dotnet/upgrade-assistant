// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers
{
    public static class TargetSyntaxMessageLoader
    {
        /// <summary>
        /// The suffix that api/message map files are expected to use.
        /// </summary>
        private const string TargetSyntaxMessageFileSuffix = ".apitargets";
        private static readonly char[] NewLineCharacters = new[] { '\n', '\r' };
        private static readonly char[] IdMessageDelimiters = new[] { ':' };
        private static readonly char[] ApiSyntaxDelimiters = new[] { ',' };

        /// <summary>
        /// Load target syntax messages from additional files.
        /// </summary>
        /// <param name="additionalTexts">The additional texts to parse for type mappings.</param>
        /// <returns>Target syntax messages as defined in *.apitargets files in the project's additional files.</returns>
        public static IEnumerable<TargetSyntaxMessage> LoadMappings(ImmutableArray<AdditionalText> additionalTexts)
        {
            foreach (var file in additionalTexts)
            {
                if (file.Path.EndsWith(TargetSyntaxMessageFileSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    var messageMaps = LoadMappings(file.GetText()?.ToString() ?? string.Empty);
                    if (messageMaps is null)
                    {
                        continue;
                    }

                    foreach (var messageMap in messageMaps)
                    {
                        yield return messageMap;
                    }
                }
            }
        }

        /// <summary>
        /// Load target syntax messages from a serialized string.
        /// </summary>
        /// <param name="serializedContents">A collection of target syntax messages stored as strings.</param>
        /// <returns>An enumerable of TargetSyntaxMessages corresponding to those serialized in the serializedContents argument.</returns>
        public static IEnumerable<TargetSyntaxMessage>? LoadMappings(string serializedContents)
        {
            // NOTE: This uses a simple text serialization because dependencies like System.Text.Json or Newtonsoft.Json
            // don't work well in analyzers (which need to run from a variety of hosts with different versions of those
            // packages (or no version at all) available. A serialization technology common between .NET Framework and .NET Core
            // like XML serialization could be used, but the serialization needs are simple enough here that just reading strings
            // works and allows the input files to be more concise.
            if (string.IsNullOrWhiteSpace(serializedContents))
            {
                return Enumerable.Empty<TargetSyntaxMessage>();
            }

            var ret = new List<TargetSyntaxMessage>();
            TargetSyntaxMessage? nextMessage = null;

            foreach (var line in serializedContents.Split(NewLineCharacters, StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim()))
            {
                // Skip blank lines and comment lines (lines starting with #)
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                // Check for new target syntax message start (ID: Message)
                else if (line.Contains(':'))
                {
                    AddNextMessageToRet();
                    var idAndMessage = line.Split(IdMessageDelimiters, 2);
                    nextMessage = new TargetSyntaxMessage(idAndMessage[0].Trim(), new List<TargetSyntax>(), idAndMessage[1].Trim());
                }

                // Check for API target syntax (API Type, API name, Alert on Ambiguous match)
                else if (nextMessage is not null)
                {
                    // RemoveEmptyEntries is specified here to allow trailing commas if users prefer that style
                    var apiTargetSyntax = line.Split(ApiSyntaxDelimiters, StringSplitOptions.RemoveEmptyEntries);
                    if (apiTargetSyntax.Length != 3)
                    {
                        continue;
                    }

                    if (!Enum.TryParse<TargetSyntaxType>(apiTargetSyntax[0].Trim(), true, out var targetSyntaxType))
                    {
                        continue;
                    }

                    if (!bool.TryParse(apiTargetSyntax[2].Trim(), out var alertOnAmbiguousMatch))
                    {
                        continue;
                    }

                    ((List<TargetSyntax>)nextMessage.TargetSyntaxes).Add(new TargetSyntax(apiTargetSyntax[1].Trim(), targetSyntaxType, alertOnAmbiguousMatch));
                }
            }

            AddNextMessageToRet();

            return ret;

            void AddNextMessageToRet()
            {
                if (nextMessage is not null)
                {
                    ret.Add(nextMessage);
                    nextMessage = null;
                }
            }
        }
    }
}
