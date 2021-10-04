// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.LooseAssembly.Client
{
    /// <summary>
    /// Provides a definition for the structure of the file header for the index file.
    /// This structure is written at offset zero of the file.
    /// </summary>
    public struct IndexHeader
    {
        /// <summary>
        /// Should be the UTF8 bytes for "CHEM" (0x4d454843).
        /// The constructor that takes a schemaVersion will initialize this.
        /// </summary>
        public uint Magic1;

        /// <summary>
        /// Should be the UTF8 bytes for "NX" (0x584e).
        /// The constructor that takes a schemaVersion will initialize this.
        /// </summary>
        public ushort Magic2;

        /// <summary>
        /// The schema version. This is a monotonically-increasing number that
        /// indicates the schema version for the file. This allows the first 8 bytes
        /// of the file to be read to ascertain whether it is an index file, and
        /// how it should be read.
        /// </summary>
        public ushort SchemaVersion;

        /// <summary>
        /// The number of Hash entries in the index.
        /// </summary>
        public uint HashCount;

        /// <summary>
        /// The size (in bytes) of the assembly name index.
        /// </summary>
        public uint AssemblyNameIndexSize;

        /// <summary>
        /// A timestamp for the time the file is written.
        /// </summary>
        public uint Timestamp; // seconds since the unix "epoch"

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
