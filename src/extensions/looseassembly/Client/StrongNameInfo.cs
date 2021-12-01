// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.LooseAssembly.Client
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

        public static StrongNameInfo? Get(Stream stream)
        {
            try
            {
                using var peReader = new PEReader(stream);

                if (!peReader.HasMetadata)
                {
                    return default;
                }

                var reader = peReader.GetMetadataReader();

                if (!reader.IsAssembly)
                {
                    return default;
                }

                var assemblyName = reader.GetAssemblyDefinition().GetAssemblyName();

                return new(assemblyName.Name, new(assemblyName.GetPublicKeyToken()));
            }
            catch (IOException)
            {
                return default;
            }
            catch (BadImageFormatException)
            {
                return default;
            }
        }

        public override string ToString() => $"{Name}, PublicKeyToken={PublicKeyToken}";
    }
}
