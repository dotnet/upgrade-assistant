// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.UsageCrawling
{
#pragma warning disable CA1036 // Override methods on comparable types
    public readonly struct ApiKey : IEquatable<ApiKey>, IComparable<ApiKey>, IComparable
#pragma warning restore CA1036 // Override methods on comparable types
    {
        public ApiKey(string documentationId)
        {
            ArgumentNullException.ThrowIfNull(documentationId);

            Id = ComputeGuid(documentationId);
            DocumentationId = documentationId;
        }

        public Guid Id { get; }

        public string DocumentationId { get; }

        public bool Equals(ApiKey other)
        {
            return Id.Equals(other.Id);
        }

        public override bool Equals(object? obj)
        {
            return obj is ApiKey other &&
                   Equals(other);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public int CompareTo(ApiKey other)
        {
            return string.CompareOrdinal(DocumentationId, other.DocumentationId);
        }

        public int CompareTo(object? obj)
        {
            if (obj is ApiKey other)
            {
                return CompareTo(other);
            }

            return -1;
        }

        private static Guid ComputeGuid(string documentationId)
        {
            const int maxBytesOnStack = 256;

            var encoding = Encoding.UTF8;
            var maxByteCount = encoding.GetMaxByteCount(documentationId.Length);

            if (maxByteCount <= maxBytesOnStack)
            {
                var buffer = (Span<byte>)stackalloc byte[maxBytesOnStack];
                var written = encoding.GetBytes(documentationId, buffer);
                var utf8Bytes = buffer[..written];
                return HashData(utf8Bytes);
            }
            else
            {
                var utf8Bytes = encoding.GetBytes(documentationId);
                return HashData(utf8Bytes);
            }
        }

        private static Guid HashData(ReadOnlySpan<byte> bytes)
        {
            var hashBytes = (Span<byte>)stackalloc byte[16];
#pragma warning disable CA5351 // Do Not Use Broken Cryptographic Algorithms
            var written = MD5.HashData(bytes, hashBytes);
#pragma warning restore CA5351 // Do Not Use Broken Cryptographic Algorithms
            Debug.Assert(written == hashBytes.Length, "Expect written data length to equal hash byte count");

            return new Guid(hashBytes);
        }

        public static bool operator ==(ApiKey left, ApiKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ApiKey left, ApiKey right)
        {
            return !(left == right);
        }
    }
}
