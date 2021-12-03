// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.LooseAssembly.Client
{
    /// <summary>
    /// Provides a definition for the structure of the file header for the index file.
    /// This structure is written at offset zero of the file.
    /// </summary>
#pragma warning disable CA1815 // Override equals and operator equals on value types
    public readonly struct IndexHeader
#pragma warning restore CA1815 // Override equals and operator equals on value types
    {
        /// <summary>
        /// Gets should be the UTF8 bytes for "CHEM" (0x4d454843).
        /// The constructor that takes a schemaVersion will initialize this.
        /// </summary>
        public uint Magic1 { get; }

        /// <summary>
        /// Gets should be the UTF8 bytes for "NX" (0x584e).
        /// The constructor that takes a schemaVersion will initialize this.
        /// </summary>
        public ushort Magic2 { get; }

        /// <summary>
        /// Gets the schema version. This is a monotonically-increasing number that
        /// indicates the schema version for the file. This allows the first 8 bytes
        /// of the file to be read to ascertain whether it is an index file, and
        /// how it should be read.
        /// </summary>
        public ushort SchemaVersion { get; }

        /// <summary>
        /// Gets the number of Hash entries in the index.
        /// </summary>
        public uint HashCount { get; }

        /// <summary>
        /// Gets the size (in bytes) of the assembly name index.
        /// </summary>
        public uint AssemblyNameIndexSize { get; }

        /// <summary>
        /// Gets a timestamp for the time the file is written.
        /// </summary>
        public uint Timestamp { get; } // seconds since the unix "epoch"

        public IndexHeader(ushort schemaVersion)
        {
            Magic1 = 0x4d454843; // UTF8 bytes for "CHEM"
            Magic2 = 0x584e; // UTF8 bytes for "NX"
            SchemaVersion = schemaVersion;
            HashCount = 0;
            AssemblyNameIndexSize = 0;
            Timestamp = 0;
        }
    }
}
