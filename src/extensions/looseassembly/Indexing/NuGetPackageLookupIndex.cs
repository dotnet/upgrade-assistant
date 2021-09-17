// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Security.Cryptography;
using System.Text;
using Marklio.Metadata;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.LooseAssembly.Indexing
{
    /// <summary>
    /// Encapsulates the operations to use a NuGet Package Lookup Index.
    /// </summary>
    public class NuGetPackageLookupIndex : IDisposable
    {
        private const double RANGE = .000002;

        /// <summary>
        /// The number of bytes that each hash record takes up (16 for hash + 4 for offset).
        /// </summary>
        private const int PERHASH_SIZE = 20;

        /// <summary>
        /// The memory-mapped file instance over the index.
        /// </summary>
        private readonly MemoryMappedFile _index;

        /// <summary>
        /// The manager for the hash lookup section of the file.
        /// </summary>
        private readonly ReadOnlyMemoryMappedManager _hashLookup;

        /// <summary>
        /// The dictionary lookup for the assembly name lookup section of the file.
        /// NOTE: this is written completely by the time the constructor exits.
        /// </summary>
        private readonly Dictionary<StrongNameInfo, uint> _assemblyNameLookup;

        /// <summary>
        /// The manager for the packages section of the file.
        /// </summary>
        private readonly ReadOnlyMemoryMappedManager _packages;

        /// <summary>
        /// Gets the number of hash entries in the file.
        /// </summary>
        public uint HashCount { get; }

        /// <summary>
        /// Gets the timestamp for the index file.
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NuGetPackageLookupIndex"/> class. Creates an index over an index file at the provided path.
        /// </summary>
        /// <remarks>
        /// This opens the file for reading, with file sharing for read access (so read handles could be opened).
        /// </remarks>
        public NuGetPackageLookupIndex(string path)
        {
            // Memory-mapped files have odd behaviors WRT the length of files.
            // Since it will create a file stream anyway, let's do it early to get precise size information
            // for sizing the sections.
            // otherwise, the last "page" will make it look like the file is larger than it is.
            var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var size = stream.Length;

            // we need to know the size of the header and the magic numbers to compare, so make one and marshal it
            var headerComp = new IndexHeader(1);

            // local function to prevent littering unsafe
            unsafe int SizeOfHeader() => sizeof(IndexHeader);
            var headerSize = SizeOfHeader();

            // TODO: as we begin evolving the schema from this point, we should read the first 8 bytes to determine
            // whether the file is an index file, and what schema version it is. Then, we can go down the appropriate paths
            // to read the file. For now, we just read the whole header.
            if (size < headerSize)
            {
                throw new InvalidOperationException("Invalid Index File. Not large enough for header.");
            }

            // Create the main memory-mapped file
            // NOTE: we pass leaveOpen as false to give lifetime management of the stream to the MMF.
            _index = MemoryMappedFile.CreateFromFile(stream, null, 0, MemoryMappedFileAccess.Read, HandleInheritability.None, leaveOpen: false);

            // TODO: Should we just slice the memory rather than partition by view accessors? It's not really buying us much

            // create a temporary manager for the header bytes
            using var header = new ReadOnlyMemoryMappedManager(_index.CreateViewAccessor(0, headerSize, MemoryMappedFileAccess.Read));

            // do validation of the header
            var indexHeader = header.GetSpan().Read<IndexHeader>();
            if (indexHeader.Magic1 != headerComp.Magic1 || indexHeader.Magic2 != headerComp.Magic2)
            {
                throw new InvalidOperationException($"Invalid Index File. Incorrect header magic (expected: {headerComp.Magic1}-{headerComp.Magic2} actual:{indexHeader.Magic1}-{indexHeader.Magic2}");
            }

            if (indexHeader.SchemaVersion != 1)
            {
                throw new NotSupportedException($"This index is a schema version that this client is unable to read. Highest schema version supported: 1 actual:{indexHeader.SchemaVersion}");
            }

            // interpret the timestamp
            // TODO: this timestamp scheme gets us to 2106 or thereabouts. Should we use something different?
            Timestamp = DateTime.UnixEpoch.AddSeconds(indexHeader.Timestamp);

            // calculate the offset to the main index and calculate it's size
            // the hash index is right after the header
            var hashIndexOffset = headerSize;
            HashCount = indexHeader.HashCount;
            var hashesSize = checked(HashCount * PERHASH_SIZE);

            // calculate the offset of the assembly name index and get it's size
            // the assembly name index is right after the hash index
            var assemblyNameIndexOffset = hashIndexOffset + hashesSize;
            var assemblyNameIndexBytes = indexHeader.AssemblyNameIndexSize;

            // calculate the offset to the packages/versions list and calculate its size (the rest of the file)
            // the packages listing is right after the assembly name index
            var packagesOffset = assemblyNameIndexOffset + assemblyNameIndexBytes;
            var packagesSize = size - packagesOffset;

            // do some validation on the size of the file. This won't flag all messed-up files, but it will catch some common problems early
            // we also spot check the size while generating the assembly name index below.
            // TODO: Consider adding a checksum to completely detect corrupt index files
            if (size <= packagesOffset)
            {
                throw new InvalidOperationException($"The file is not large enough to hold the data described by the header. The file may have been truncated.");
            }

            // create managers for the various sections of the index, being careful to get sizes correct
            _hashLookup = new ReadOnlyMemoryMappedManager(_index.CreateViewAccessor(hashIndexOffset, hashesSize, MemoryMappedFileAccess.Read));

            // we will read all the contents of the assembly name index, so we don't need to save it past construction
            using var assemblyNameLookup = new ReadOnlyMemoryMappedManager(_index.CreateViewAccessor(assemblyNameIndexOffset, assemblyNameIndexBytes, MemoryMappedFileAccess.Read));
            _packages = new ReadOnlyMemoryMappedManager(_index.CreateViewAccessor(packagesOffset, packagesSize, MemoryMappedFileAccess.Read));

            // read the assembly name index into memory, since lookups can't be on quickly as-is
            // NOTE: the use of inline function is useful to get us an iterator
            IEnumerable<KeyValuePair<StrongNameInfo, uint>> EnumerateAssemblyNameEntries()
            {
                var memory = assemblyNameLookup.Memory;

                // loop until we've read all the memory
                while (memory.Length > 0)
                {
                    // read a byte and advance our "cursor".
                    // We'll follow this pattern for consuming the memory.
                    var firstByte = memory.Span[0];
                    memory = memory[1..];

                    // decode the data in the first byte
                    var pktIsPresent = (firstByte & (1 << 7)) != 0;
                    var nameIsShort = (firstByte & (1 << 6)) != 0;
                    var firstByteValue = firstByte & 0x3f;
                    var length = 0;
                    if (nameIsShort)
                    {
                        length = firstByteValue;
                    }
                    else
                    {
                        // read another byte to decode the full length
                        var secondByte = memory.Span[0];
                        memory = memory[1..];
                        length = (firstByteValue << 8) | secondByte;
                    }

                    // slice out the length for the string
                    var buffer = memory.Span.Slice(0, length);
                    memory = memory[length..];

                    // decode the string
                    var name = Encoding.UTF8.GetString(buffer);

                    // decode the PKT if present
                    PublicKeyToken? pkt = null;
                    if (pktIsPresent)
                    {
                        pkt = new PublicKeyToken(memory.Span.Slice(0, 8));
                        memory = memory[8..];
                    }

                    // read the offset
                    var offset = memory.Span.Read<uint>();
                    if (packagesOffset + offset > size)
                    {
                        throw new InvalidOperationException($"The index contains offsets beyond the end of the file. The index is likely corrupt or has been truncated");
                    }

                    memory = memory[4..];

                    // yield this entry
                    yield return pkt is null ? new(new(name), offset) : new(new(name, pkt.Value), offset);
                }
            }

            // read the lookup using our function
            _assemblyNameLookup = new(EnumerateAssemblyNameEntries());
        }

        /// <summary>
        /// Enumerates the hash entries in the file. This is mainly useful for testing the integrity of the index.
        /// </summary>
        public IEnumerable<(HashToken HashToken, NuGetPackageVersion PackageVersion)> EnumerateHashTokenEntries()
        {
            var hashLookupMemory = _hashLookup.Memory;

            // our approach here is to just walk the bytes, slicing off the front each time we read a record
            while (hashLookupMemory.Length > 0)
            {
                // read the hash token from the memory
                var hashToken = HashToken.FromTokenBytes(hashLookupMemory.Span.Slice(0, 16));

                // read the package offset from the memory
                var packageOffset = hashLookupMemory.Span.Slice(16, 4).Read<uint>();
                Debug.Assert(packageOffset != uint.MaxValue, "A bogus package offset made it into the index");

                // Look up the package and yield the entry
                yield return (hashToken, GetNuGetPackageVersionFromOffset(packageOffset));

                // slice off the entry
                hashLookupMemory = hashLookupMemory[PERHASH_SIZE..];
            }
        }

        /// <summary>
        /// Enumerates the assembly name owner package entries in the file. This is mainly useful for testing the integrity of the index.
        /// </summary>
        public IEnumerable<(StrongNameInfo StrongName, string OwningPackageId)> EnumerateAssemblyNameOwnerPackageEntries()
        {
            // just loop through the lookup entries and render the offsets as package ids
            foreach (var (strongName, offset) in _assemblyNameLookup)
            {
                yield return (strongName, GetPackageIdFromOffset(offset));
            }
        }

        /// <summary>
        /// Gets a package ID from an offset.
        /// </summary>
        /// <remarks>
        /// The offset is expected to point to the package id "marker", so the string isn't expected to begin until the 3rd byte.
        /// </remarks>
        private string GetPackageIdFromOffset(uint packageIdOffset)
        {
            // TODO: validate the package marker?
            var intOffset = checked((int)(packageIdOffset + 2)); // NOTE: the offsets that come out of the index point to the package id prefix marker, so we have to skip it (+2)
            return _packages.GetSpan()[intOffset..].ReadNullTerminatedUtf8String(out _);
        }

        /// <summary>
        /// Enumerates the package/version entries in the file. This is mainly useful for testing the integrity of the index.
        /// </summary>
        /// <remarks>
        /// Note, if there are any rooted package ids without versions, this will not enumerate them.
        /// </remarks>
        public IEnumerable<NuGetPackageVersion> EnumeratePackageEntries()
        {
            var packagesMemory = _packages.Memory;

            // this holds the "current" package id, since this isn't duplicated for each version
            var currentPackageId = default(string);
            while (packagesMemory.Length > 0)
            {
                int consumed;

                // check to see if we're at a package id (they start with 0xffff)
                if (packagesMemory.Length >= 2 && packagesMemory.Span[0] == 0xff && packagesMemory.Span[1] == 0xff)
                {
                    // slice off the 0xffff
                    packagesMemory = packagesMemory[2..];

                    // read the string and consume the memory
                    currentPackageId = packagesMemory.Span.ReadNullTerminatedUtf8String(out consumed);
                    packagesMemory = packagesMemory[consumed..];

                    // continue in case there is a rooted package id with no versions
                    continue;
                }

                // read the version
                // TODO: check for malformed 0xffff here?
                var version = packagesMemory.Span.ReadNullTerminatedUtf8String(out consumed);

                // there was a problem early on with zero-length versions. Keep this check for now
                if (version.Length == 0)
                {
                    throw new InvalidOperationException("zero-length version");
                }

                // consume the memory
                packagesMemory = packagesMemory[consumed..];
                if (currentPackageId == null)
                {
                    throw new InvalidOperationException("Null package id!");
                }

                // yield the entry
                yield return new NuGetPackageVersion(currentPackageId, version);
            }
        }

        /// <summary>
        /// Finds the package that contains the file.
        /// </summary>
        /// <param name="assemblyNameOwnerId">If an "owning package" for the assembly name was found, this will be non-null.</param>
        /// <param name="containingPackage">If there was a precise match for the file, this is the earliest NuGet package version in which it was found.</param>
        public void FindNuGetPackageInfoForFile(string filePath, out string? assemblyNameOwnerId, out NuGetPackageVersion? containingPackage)
        {
            using var file = new FileExplorer(filePath);
            FindNuGetPackageInfoForFile(file, out assemblyNameOwnerId, out containingPackage);
        }

        /// <summary>
        /// Finds the package that contains the file.
        /// </summary>
        /// <param name="assemblyNameOwnerId">If an "owning package" for the assembly name was found, this will be non-null.</param>
        /// <param name="containingPackage">If there was a precise match for the file, this is the earliest NuGet package version in which it was found.</param>
        public void FindNuGetPackageInfoForFile(Stream fileStream, out string? assemblyNameOwnerId, out NuGetPackageVersion? containingPackage)
        {
            using var file = new FileExplorer(fileStream);
            FindNuGetPackageInfoForFile(file, out assemblyNameOwnerId, out containingPackage);
        }

        private void FindNuGetPackageInfoForFile(FileExplorer file, out string? assemblyNameOwnerId, out NuGetPackageVersion? containingPackage)
        {
            assemblyNameOwnerId = null;
            containingPackage = null;

            // we need to do some validation on the file to see if it is viable before trying to do a lookup
            // we won't have any non-PEs
            if (!file.IsPE)
            {
                return;
            }

            using var pe = new PEExplorer(file);

            // we won't have any non-assemblies
            if (!pe.HasClrData)
            {
                return;
            }

            // we need to construct the strong name, so open up the metadata and pull the assembly info out
            using var md = new MDSkimmer(pe);
            var assembly = new AssemblyToken(md, 1).GetRow();
            var assemblyName = assembly.Name.ToString();
            StrongNameInfo strongName;
            if (assembly.PublicKey.Value == 0)
            {
                strongName = new(assemblyName);
            }
            else
            {
                var publicKey = assembly.PublicKey.GetBlob();
                if (publicKey.Length == 8)
                {
                    // if it's 8 long, it's just the PKT
                    strongName = new(assemblyName, new(publicKey));
                }
                else
                {
                    // otherwise, we need to construct the PKT (last 8 bytes of SHA1 hash of public key)
                    strongName = new(assemblyName, PublicKeyToken.FromPublicKey(publicKey));
                }
            }

            // TODO: lookup whether it is a Framework or other "well-known" binary.
            // get the hash from the file.
            var fullHash = file.GetHash(SHA256.Create());
            var hashToken = HashToken.FromSha256Bytes(fullHash);

            // look it up in the index
            if (TrySmartSearchForHashToken(hashToken, out var entry, out var probes))
            {
                var offset = entry[16..].Read<uint>();
                containingPackage = GetNuGetPackageVersionFromOffset(offset);
            }

            // look up the assembly name in that index
            if (_assemblyNameLookup.TryGetValue(strongName, out var packageOffset))
            {
                assemblyNameOwnerId = GetPackageIdFromOffset(packageOffset);
            }
        }

        /// <summary>
        /// Looks up a package version from an entry offset.
        /// </summary>
        private NuGetPackageVersion GetNuGetPackageVersionFromOffset(uint offset)
        {
            var packages = _packages.GetSpan();
            var intOffset = checked((int)offset);

            // we should be at the version
            var versionString = packages[intOffset..].ReadNullTerminatedUtf8String(out _);

            // now we need to look for the package name
            // it's behind us, so walk back looking for 0xffff
            var state = 0; // 0 - looking, 1 - found 0xff, 2 - found 0xff while in state 1
            var i = intOffset - 1;
            int packageOffset = -1;

            // go until we find it
            // TODO: do we need an escape? Or is crashing on a malformed index fine?
            while (packageOffset == -1)
            {
                // this is a state machine that looks for 0xffff, which marks the package id
                switch (packages[i])
                {
                    case 0xff:
                        state++;
                        if (state == 2)
                        {
                            packageOffset = i + 2;
                        }

                        break;
                    default:
                        state = 0;
                        break;
                }

                i--;
            }

            // read the string from the package offset
            var id = packages[packageOffset..].ReadNullTerminatedUtf8String(out _);

            // construct the version
            return new NuGetPackageVersion(id, versionString);
        }

        /// <summary>
        /// This is our "smart" lookup algorithm that uses it's knowledge of the keyspace to do better than a simple binary search.
        /// TODO: tune this.
        /// </summary>
        private bool TrySmartSearchForHashToken(HashToken hashToken, out ReadOnlySpan<byte> entry, out ushort probes)
        {
            probes = 0;

            // calculate where we should be in the keyspace
            var keyspaceLocation = hashToken.GetKeySpaceLocation();

            // calculate a likely index based on that
            var index = (int)(HashCount * keyspaceLocation);

            // check that location
            probes++;
            var lookupSpan = _hashLookup.GetSpan();
            var entrySpan = lookupSpan.Slice(index * PERHASH_SIZE, PERHASH_SIZE);
            var compareValue = hashToken.CompareTo(entrySpan);
            if (compareValue == 0)
            {
                // holy moly, we found it!
                entry = entrySpan;
                return true;
            }
            else if (compareValue < 0)
            {
                // we might need to go farther, so loop here
                while (true)
                {
                    // carve out a probability segment to the "left"
                    // we need to decide how far to go. Let's try %1
                    var newKeySpaceLocation = keyspaceLocation - RANGE;
                    var newIndex = (int)(HashCount * newKeySpaceLocation);
                    if (newIndex < 0)
                    {
                        newIndex = 0;
                    }

                    probes++;
                    var newEntrySpan = lookupSpan.Slice(newIndex * PERHASH_SIZE, PERHASH_SIZE);
                    var newCompareValue = hashToken.CompareTo(newEntrySpan);
                    if (newCompareValue == 0)
                    {
                        entry = newEntrySpan;
                        return true;
                    }

                    if (newCompareValue < 0)
                    {
                        // we still missed it, try again
                        index = newIndex;
                        keyspaceLocation = newKeySpaceLocation;
                    }
                    else
                    {
                        // ok, lets binary search between them
                        var result = TryBinarySearchForHashToken(lookupSpan[((newIndex + 1) * PERHASH_SIZE)..((index - 1) * PERHASH_SIZE)], hashToken, out entry, out var newProbes);
                        probes += newProbes;
                        return result;
                    }
                }
            }
            else
            {
                // we might need to go farther, so loop here
                while (true)
                {
                    // carve out a probability segment to the "right"
                    // we need to decide how far to go. Let's try %1
                    var newKeySpaceLocation = keyspaceLocation + RANGE;
                    var newIndex = checked((int)(HashCount * newKeySpaceLocation));
                    if (newIndex >= HashCount)
                    {
                        newIndex = (int)(HashCount - 1);
                    }

                    probes++;
                    var newEntrySpan = lookupSpan.Slice(newIndex * PERHASH_SIZE, PERHASH_SIZE);
                    var newCompareValue = hashToken.CompareTo(newEntrySpan);
                    if (newCompareValue == 0)
                    {
                        entry = newEntrySpan;
                        return true;
                    }
                    else if (newCompareValue > 0)
                    {
                        // we still missed it, try again
                        index = newIndex;
                        keyspaceLocation = newKeySpaceLocation;
                    }
                    else
                    {
                        // ok, lets binary search between them
                        var result = TryBinarySearchForHashToken(lookupSpan[((index + 1) * PERHASH_SIZE)..((newIndex - 1) * PERHASH_SIZE)], hashToken, out entry, out var newProbes);
                        probes += newProbes;
                        return result;
                    }
                }
            }
        }

        /// <summary>
        /// Implements the search for a given hash token.
        /// </summary>
        private bool TryBinarySearchForHashToken(ReadOnlySpan<byte> indexSegment, HashToken hashToken, out ReadOnlySpan<byte> entry, out ushort probes)
        {
            probes = 0;
            if (indexSegment.Length % PERHASH_SIZE != 0)
            {
                throw new InvalidOperationException("hash index size misalignment.");
            }

            while (true)
            {
                probes++;
                var count = indexSegment.Length / PERHASH_SIZE;
                var index = (int)(count / 2);
                var entrySpan = indexSegment.Slice(index * PERHASH_SIZE, PERHASH_SIZE);
                var compareValue = hashToken.CompareTo(entrySpan.Slice(0, 16));
                if (compareValue == 0)
                {
                    // we found it!
                    entry = entrySpan;
                    return true;
                }
                else if (compareValue < 0)
                {
                    indexSegment = indexSegment.Slice(0, index * PERHASH_SIZE);
                }
                else
                {
                    indexSegment = indexSegment[((index + 1) * PERHASH_SIZE)..];
                }

                if (indexSegment.Length == 0)
                {
                    entry = indexSegment;
                    return false;
                }
            }
        }

        public virtual void Dispose()
        {
            _hashLookup.Dispose();
            _packages.Dispose();
            _index.Dispose();
        }
    }
}
