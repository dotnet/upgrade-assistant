// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.UpgradeAssistant
{
    internal class JsonStringProjectItemTypeConverter : JsonConverter<ProjectItemType>
    {
        public override ProjectItemType? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var name = reader.GetString();
            return name is null
                ? null
                : new ProjectItemType(name);
        }

        public override void Write(Utf8JsonWriter writer, ProjectItemType value, JsonSerializerOptions options) =>
            writer?.WriteStringValue(value?.Name ?? throw new ArgumentNullException(nameof(value)));
    }
}
