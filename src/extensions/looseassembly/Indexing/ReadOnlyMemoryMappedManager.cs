// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO.MemoryMappedFiles;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.LooseAssembly.Indexing
{
    /// <summary>
    /// This provides a read-only-enforced view over a MemoryMappedViewAccessor.
    /// </summary>
    internal class ReadOnlyMemoryMappedManager : IDisposable
    {
        /// <summary>
        /// The underlying MemoryMappedManager.
        /// </summary>
        private readonly MemoryMappedManager _manager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadOnlyMemoryMappedManager"/> class.
        /// </summary>
        public ReadOnlyMemoryMappedManager(MemoryMappedViewAccessor view)
        {
            _manager = new(view, allowReadOnly: true);
        }

        /// <summary>
        /// Gets the read-only memory.
        /// </summary>
        public ReadOnlyMemory<byte> Memory => _manager.Memory;

        /// <summary>
        /// Mirrors MemoryMappedManager.GetSpan() with a readonly span.
        /// </summary>
        public ReadOnlySpan<byte> GetSpan() => _manager.GetSpan();

        public void Dispose()
        {
            ((IDisposable)_manager).Dispose();
        }
    }
}
