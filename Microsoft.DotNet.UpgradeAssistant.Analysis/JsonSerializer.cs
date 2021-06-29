// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Microsoft.DotNet.UpgradeAssistant.Analysis
{
    public class JsonSerializer : IJsonSerializer
    {
        public Encoding FileEncoding { get; set; } = new UTF8Encoding(false);

        public JsonSerializerSettings Settings { get; set; } = new JsonSerializerSettings()
        {
            Formatting = Formatting.Indented,
            Culture = CultureInfo.InvariantCulture,
            TypeNameHandling = TypeNameHandling.None,
            NullValueHandling = NullValueHandling.Ignore
        };

        public JsonSerializer()
        {
        }

        public virtual T Deserialize<T>(string content)
        {
            return JsonConvert.DeserializeObject<T>(content);
        }

        public virtual string Serialize<T>(T obj)
        {
            return JsonConvert.SerializeObject(obj, typeof(T), Settings);
        }

        public virtual T Read<T>(string filePath)
        {
            if (!File.Exists(filePath))
            {
#pragma warning disable CA2201 // Do not raise reserved exception types
                throw new Exception(filePath);
#pragma warning restore CA2201 // Do not raise reserved exception types
            }

            string contents = File.ReadAllText(filePath);
            return Deserialize<T>(contents);
        }

        public virtual void Write<T>(string filePath, T obj)
        {
            string contents = Serialize<T>(obj);
            File.WriteAllText(filePath, contents);
        }

        public virtual void Write<T>(string filePath, T obj, bool ensureDirectory)
        {
            string contents = Serialize<T>(obj);

            if (ensureDirectory)
            {
                EnsureDirectoryForFilePath(filePath);
            }

            File.WriteAllText(filePath, contents);
        }

        internal virtual bool EnsureDirectoryForFilePath(string filePath)
        {
            string directoryName = Path.GetDirectoryName(filePath);

            if (!string.IsNullOrWhiteSpace(directoryName))
            {
                return Directory.Exists(directoryName);
            }

            return false;
        }
    }
}
