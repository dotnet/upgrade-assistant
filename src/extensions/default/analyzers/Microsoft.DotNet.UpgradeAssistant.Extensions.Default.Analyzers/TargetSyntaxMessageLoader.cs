// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers
{
    public static class TargetSyntaxMessageLoader
    {
        /// <summary>
        /// The suffix that api/message map files are expected to use.
        /// </summary>
        private const string TargetSyntaxMessageFileSuffix = ".apitargets";

        private static JsonSerializerOptions GetJsonSerializationOptions()
        {
            var jsonSerializerOptions = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
            };

            jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

            return jsonSerializerOptions;
        }

        /// <summary>
        /// Load target syntax messages from a serialized string.
        /// </summary>
        /// <param name="serializedContents">A JSON-serialized array of target syntax messages.</param>
        /// <returns>An enumerable of TargetSyntaxMessages corresponding to those serialized in the serializedContents argument.</returns>
        public static IEnumerable<TargetSyntaxMessage>? LoadMappings(string serializedContents) =>
            JsonSerializer.Deserialize<TargetSyntaxMessage[]>(serializedContents, GetJsonSerializationOptions());

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
                    var messageMaps = LoadMappings(file.GetText()?.ToString() ?? "[]");
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
    }
}
