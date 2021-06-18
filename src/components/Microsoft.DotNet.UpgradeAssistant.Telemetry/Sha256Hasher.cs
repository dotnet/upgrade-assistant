// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.DotNet.UpgradeAssistant.Telemetry
{
    public class Sha256Hasher : IStringHasher
    {
        public virtual string Hash(string text)
        {
            using var sha256 = SHA256.Create();

            var bytes = Encoding.UTF8.GetBytes(text);
            var hash = sha256.ComputeHash(bytes);
            var hashString = new StringBuilder();

            foreach (var x in hash)
            {
                hashString.AppendFormat(CultureInfo.InvariantCulture, "{0:x2}", x);
            }

            return hashString.ToString();
        }
    }
}
