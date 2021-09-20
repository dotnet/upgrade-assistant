// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.LooseAssembly.Client
{
    public readonly struct LookupToken
    {
        public LookupToken(StrongNameInfo strongName, HashToken token)
        {
            StrongName = strongName;
            HashToken = token;
        }

        public StrongNameInfo StrongName { get; }

        public HashToken HashToken { get; }

        public static LookupToken? Create(string fileName)
        {
            using var stream = File.OpenRead(fileName);

            var strongName = StrongNameInfo.GetStrongName(stream);

            if (strongName is null)
            {
                return default;
            }

            stream.Position = 0;
            var hashToken = HashToken.FromStream(stream);

            return new(strongName, hashToken);
        }
    }
}
