// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.IO;
using Newtonsoft.Json;

namespace Microsoft.DotNet.UpgradeAssistant.Analysis
{
    public class JsonSerializer : ISerializer
    {
        private readonly Newtonsoft.Json.JsonSerializer _serializer = Newtonsoft.Json.JsonSerializer.Create(new()
        {
            Formatting = Formatting.Indented,
            Culture = CultureInfo.InvariantCulture,
            TypeNameHandling = TypeNameHandling.None,
            NullValueHandling = NullValueHandling.Ignore,
        });

        public virtual string Serialize<T>(T obj)
        {
            using var stringWriter = new StringWriter();

            Write<T>(stringWriter, obj);

            return stringWriter.ToString();
        }

        public void Write<T>(TextWriter writer, T obj)
        {
            using var jsonWriter = new JsonTextWriter(writer);

            _serializer.Serialize(jsonWriter, obj, typeof(T));
        }
    }
}
