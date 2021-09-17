// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.IO.MemoryMappedFiles;

namespace Chem.Indexing.Client
{
    /// <summary>
    /// This class implements MemoryManager over a memory-mapped view accessor.
    /// </summary>
    internal class MemoryMappedManager : MemoryManager<byte>
    {
        /// <summary>
        /// The underlying view accessor.
        /// </summary>
        private readonly MemoryMappedViewAccessor _View;

        /// <summary>
        /// The pointer to the mapped memory.
        /// </summary>
        private unsafe readonly byte* _Pointer;

        /// <summary>
        /// Creates a manager over an accessor.
        /// </summary>
        public unsafe MemoryMappedManager(MemoryMappedViewAccessor view) : this(view, allowReadOnly: false) { }

        /// <summary>
        /// "internal" constructor intended to protect us against ourselves.
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

            _View = view;
            _View.SafeMemoryMappedViewHandle.AcquirePointer(ref _Pointer);
        }

        /// <summary>
        /// This does the work of getting at the memory and exposing it through Memory/Span.
        /// </summary>
        /// <returns></returns>
        public unsafe override Span<byte> GetSpan()
        {
            //we need to do some math to make sure we're starting at the real bytes (due to page boundaries, etc.)
            return new Span<byte>(_Pointer + _View.PointerOffset, checked((int)_View.Capacity));
        }

        public override MemoryHandle Pin(int elementIndex = 0)
        {
            throw new NotSupportedException("Pinning isn't supported and is unnecessary for memory-mapped files");
        }

        public override void Unpin()
        {
            //no-op. No pinning actually takes place here
        }

        protected override void Dispose(bool disposing)
        {
            _View.SafeMemoryMappedViewHandle.ReleasePointer();
            _View.Dispose();
        }
    }

    /// <summary>
    /// This provides a read-only-enforced view over a MemoryMappedViewAccessor.
    /// </summary>
    internal class ReadOnlyMemoryMappedManager : IDisposable
    {
        /// <summary>
        /// The underlying MemoryMappedManager.
        /// </summary>
        private readonly MemoryMappedManager _Manager;

        /// <summary>
        /// Creates a read-only manager over the accessor.
        /// </summary>
        /// <param name="view"></param>
        public ReadOnlyMemoryMappedManager(MemoryMappedViewAccessor view)
        {
            _Manager = new(view, allowReadOnly: true);
        }

        /// <summary>
        /// The read-only memory.
        /// </summary>
        public ReadOnlyMemory<byte> Memory => _Manager.Memory;

        /// <summary>
        /// Mirrors MemoryMappedManager.GetSpan() with a readonly span.
        /// </summary>
        /// <returns></returns>
        public ReadOnlySpan<byte> GetSpan() => _Manager.GetSpan();

        public void Dispose()
        {
            ((IDisposable)_Manager).Dispose();
        }
    }
}
