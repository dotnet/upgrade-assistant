// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.LooseAssembly.Client
{
    /// <summary>
    /// Represents a hash token used to establish content identity for a file.
    /// </summary>
    public readonly struct HashToken : IEquatable<HashToken>, IComparable<HashToken>
    {
        private static readonly SHA256 Sha256 = SHA256.Create();

        /// <summary>
        /// The underlying bytes of the hash token
        /// NOTE: These are converted to local endianness, so they have appropriate values for comparisons.
        /// </summary>
        private readonly ulong _lowBytes;
        private readonly ulong _highBytes;

        /// <summary>
        /// Creates a token from a full SHA256 hash hex string.
        /// </summary>
        public static HashToken FromSha256String(string sha256Hash)
        {
            var hashTokenStringSpan = sha256Hash.AsSpan()[^32..];
            Span<byte> bytes = stackalloc byte[16];
            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = byte.Parse(hashTokenStringSpan.Slice(i * 2, 2), NumberStyles.HexNumber);
            }

            return FromTokenBytes(bytes);
        }

        private HashToken(ulong highBytes, ulong lowBytes)
        {
            _highBytes = highBytes;
            _lowBytes = lowBytes;
        }

        /// <summary>
        /// Creates a token from a full SHA256 hash.
        /// </summary>
        public static HashToken FromSha256Bytes(byte[] sha256Hash)
        {
            var tokenSpan = sha256Hash.AsSpan()[^16..];
            return FromTokenBytes(tokenSpan);
        }

        public static HashToken FromStream(Stream stream)
            => FromSha256Bytes(Sha256.ComputeHash(stream));

        /// <summary>
        /// Creates a token from the byte representation.
        /// </summary>
        private static HashToken FromTokenBytes(ReadOnlySpan<byte> tokenBytes, bool allowLongerThan16Bytes)
        {
            if (allowLongerThan16Bytes)
            {
                if (tokenBytes.Length < 16)
                {
                    throw new ArgumentException("Token bytes must be at least 16 bytes in length.", nameof(tokenBytes));
                }
            }
            else if (tokenBytes.Length != 16)
            {
                throw new ArgumentException("Token bytes must be 16 bytes in length.", nameof(tokenBytes));
            }

            var highLong = BinaryPrimitives.ReadUInt64BigEndian(tokenBytes);
            var lowLong = BinaryPrimitives.ReadUInt64BigEndian(tokenBytes[8..]);
            return new HashToken(highLong, lowLong);
        }

        public static HashToken FromTokenBytes(ReadOnlySpan<byte> tokenBytes) => FromTokenBytes(tokenBytes, allowLongerThan16Bytes: false);

        /// <summary>
        /// Creates a token from the byte representation.
        /// </summary>
        public static HashToken FromTokenBytes(byte[] tokenBytes) => FromTokenBytes(tokenBytes.AsSpan());

        /// <summary>
        /// Indicates the position across the keyspace that the hash is located.
        /// This is helpful in selecting a starting probe of an ordered index.
        /// </summary>
        public double GetKeySpaceLocation()
        {
            return _highBytes / (double)ulong.MaxValue;
        }

        /// <summary>
        /// Compares to another token.
        /// </summary>
        public int CompareTo(HashToken other)
        {
            var value = _highBytes.CompareTo(other._highBytes);
            if (value != 0)
            {
                return value;
            }

            return _lowBytes.CompareTo(other._lowBytes);
        }

        /// <summary>
        /// Compares to another token represented by a span.
        /// </summary>
        public int CompareTo(ReadOnlySpan<byte> other) => CompareTo(FromTokenBytes(other, allowLongerThan16Bytes: true));

        /// <summary>
        /// Implements equality on a token.
        /// </summary>
        public override bool Equals(object? obj)
        {
            if (obj is not HashToken)
            {
                return false;
            }

            return Equals((HashToken)obj);
        }

        /// <summary>
        /// Implements equality on a token.
        /// </summary>
        public bool Equals(HashToken other)
        {
            return _highBytes == other._highBytes && _lowBytes == other._lowBytes;
        }

        /// <summary>
        /// Gets the bytes of a token as a read-only span.
        /// </summary>
        public ReadOnlySpan<byte> GetBytes()
        {
            Span<byte> bytes = new byte[16];
            BinaryPrimitives.WriteUInt64BigEndian(bytes, _highBytes);
            BinaryPrimitives.WriteUInt64BigEndian(bytes[8..], _lowBytes);
            return bytes;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_highBytes, _lowBytes);
        }

        public override string ToString()
        {
            var builder = new StringBuilder(32);
            foreach (var b in GetBytes())
            {
                builder.Append($"{b:x2}");
            }

            return builder.ToString();
        }
    }
}
