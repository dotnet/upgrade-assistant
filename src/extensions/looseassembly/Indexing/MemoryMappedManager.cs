// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.IO.MemoryMappedFiles;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.LooseAssembly.Indexing
{
    /// <summary>
    /// This class implements MemoryManager over a memory-mapped view accessor.
    /// </summary>
    internal class MemoryMappedManager : MemoryManager<byte>
    {
        /// <summary>
        /// The underlying view accessor.
        /// </summary>
        private readonly MemoryMappedViewAccessor _view;

        /// <summary>
        /// The pointer to the mapped memory.
        /// </summary>
        private unsafe readonly byte* _pointer;

        public unsafe MemoryMappedManager(MemoryMappedViewAccessor view)
            : this(view, allowReadOnly: false) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryMappedManager"/> class.
        /// This "internal" constructor is intended to protect us against ourselves.
        /// NOTE: since the type is already internal, there's no real protection here
        /// without nesting this inside read and write versions. Since that's not helpful
        /// in this context, we've just added this here to prevent us from making a mistake
        /// accidentally.
        /// </summary>
        internal unsafe MemoryMappedManager(MemoryMappedViewAccessor view, bool allowReadOnly)
        {
            if (!view.CanWrite && !allowReadOnly)
            {
                throw new InvalidOperationException($"The provided view cannot be written to. Use ReadOnlyMemoryMappedManager instead.");
            }

            _view = view;
            _view.SafeMemoryMappedViewHandle.AcquirePointer(ref _pointer);
        }

        /// <summary>
        /// This does the work of getting at the memory and exposing it through Memory/Span.
        /// </summary>
        public unsafe override Span<byte> GetSpan()
        {
            // we need to do some math to make sure we're starting at the real bytes (due to page boundaries, etc.)
            return new Span<byte>(_pointer + _view.PointerOffset, checked((int)_view.Capacity));
        }

        public override MemoryHandle Pin(int elementIndex = 0)
        {
            throw new NotSupportedException("Pinning isn't supported and is unnecessary for memory-mapped files");
        }

        public override void Unpin()
        {
            // no-op. No pinning actually takes place here
        }

        protected override void Dispose(bool disposing)
        {
            _view.SafeMemoryMappedViewHandle.ReleasePointer();
            _view.Dispose();
        }
    }
}
