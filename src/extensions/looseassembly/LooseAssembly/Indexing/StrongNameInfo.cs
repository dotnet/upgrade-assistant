// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Chem.Indexing.Client
{
    /// <summary>
    /// Encapsulates a public key token.
    /// </summary>
    public struct PublicKeyToken : IEquatable<PublicKeyToken>
    {
        /// <summary>
        /// We need SHA1 to make PKTs from public keys.
        /// </summary>
        private static readonly SHA1 Sha1 = SHA1.Create();

        /// <summary>
        /// The underlying token stored as a ulong.
        /// </summary>
        /// <remarks>
        /// NOTE: this is stored in "network order" and the number shouldn't be used to compare across architectures.
        /// </remarks>
        private readonly ulong _pKT;

        public PublicKeyToken(ReadOnlySpan<byte> publicKeyToken)
        {
            if (publicKeyToken.Length != 8)
            {
                throw new ArgumentException($"Public key tokens must be 8 bytes long", nameof(publicKeyToken));
            }

            _pKT = publicKeyToken.Read<ulong>();
        }

        public ReadOnlySpan<byte> Bytes => MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref this, 1));

        public bool Equals(PublicKeyToken other) => _pKT == other._pKT;

        public override bool Equals(object? obj) => obj switch
        {
            PublicKeyToken pkt => Equals(pkt),
            _ => false,
        };

        public override int GetHashCode() => _pKT.GetHashCode();

        public override string ToString()
        {
            // TODO: consolidate with ToHexString(). Leaving for now due to ReadOnlySpan not implementing IEnumerable
            var builder = new StringBuilder(16);
            foreach (var b in Bytes)
            {
                builder.AppendFormat("{0:x2}", b);
            }

            return builder.ToString();
        }

        public static bool operator ==(PublicKeyToken first, PublicKeyToken second) => first.Equals(second);

        public static bool operator !=(PublicKeyToken first, PublicKeyToken second) => !first.Equals(second);

        /// <summary>
        /// Creates a PublicKeyToken from a 16-hex-character string representation.
        /// </summary>
        public static PublicKeyToken FromPktString(string pkt)
        {
            if (pkt.Length != 16)
            {
                throw new InvalidOperationException("The key length is not even");
            }

            var pktSpan = pkt.AsSpan();
            var bytes = new byte[8];
            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = byte.Parse(pktSpan.Slice(0, 2), System.Globalization.NumberStyles.HexNumber);
                pktSpan = pktSpan[2..];
            }

            return new PublicKeyToken(bytes);
        }

        /// <summary>
        /// Creates a PublicKeyToken from a long form full public key string (hex characters).
        /// </summary>
        public static PublicKeyToken FromPublicKeyString(string publicKey)
        {
            if (publicKey.Length % 2 != 0)
            {
                throw new InvalidOperationException("The key length is not even");
            }

            var keySpan = publicKey.AsSpan();
            var bytes = new byte[publicKey.Length / 2];
            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = byte.Parse(keySpan.Slice(0, 2), System.Globalization.NumberStyles.HexNumber);
                keySpan = keySpan[2..];
            }

            return FromPublicKey(bytes);
        }

        public static PublicKeyToken FromPublicKey(byte[] publicKey)
        {
            var hash = Sha1.ComputeHash(publicKey);
            return new PublicKeyToken(hash[^8..].Reverse().ToArray());
        }
    }

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
