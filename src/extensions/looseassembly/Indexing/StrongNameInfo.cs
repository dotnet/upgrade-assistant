// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.LooseAssembly.Indexing
{
    /// <summary>
    /// Encapsulates a strong name "family" identity (name and public key token).
    /// </summary>
    public class StrongNameInfo : IEquatable<StrongNameInfo>
    {
        public string Name { get; }

        public PublicKeyToken? PublicKeyToken { get; }

        public StrongNameInfo(string name, PublicKeyToken publicKeyToken)
        {
            Name = name;
            PublicKeyToken = publicKeyToken;
        }

        public StrongNameInfo(string name)
        {
            Name = name;
            PublicKeyToken = null;
        }

        public bool Equals(StrongNameInfo? other) => Name.Equals(other?.Name, StringComparison.OrdinalIgnoreCase) && PublicKeyToken == other?.PublicKeyToken;

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, PublicKeyToken);
        }

        public override bool Equals(object? obj) => obj switch
        {
            StrongNameInfo sn => Equals(sn),
            _ => false,
        };

        public override string ToString() => $"{Name}, PublicKeyToken={PublicKeyToken}";

        public static bool operator ==(StrongNameInfo first, StrongNameInfo second) => first.Equals(second);

        public static bool operator !=(StrongNameInfo first, StrongNameInfo second) => !first.Equals(second);
    }
}
