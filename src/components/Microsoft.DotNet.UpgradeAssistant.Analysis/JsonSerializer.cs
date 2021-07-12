// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace Microsoft.DotNet.UpgradeAssistant.Analysis
{
    public class JsonSerializer : ISerializer
    {
        private readonly Encoding FileEncoding = new UTF8Encoding(false);

        private readonly JsonSerializerSettings _settings = new()
        {
            Formatting = Formatting.Indented,
            Culture = CultureInfo.InvariantCulture,
            TypeNameHandling = TypeNameHandling.None,
            NullValueHandling = NullValueHandling.Ignore,
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
            return JsonConvert.SerializeObject(obj, typeof(T), _settings);
        }

        public virtual T Read<T>(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(filePath);
            }

            string contents = File.ReadAllText(filePath);
            return Deserialize<T>(contents);
        }

        public virtual void Write<T>(string filePath, T obj)
        {
            string contents = Serialize<T>(obj);
            File.WriteAllText(filePath, contents);
        }
    }
}
