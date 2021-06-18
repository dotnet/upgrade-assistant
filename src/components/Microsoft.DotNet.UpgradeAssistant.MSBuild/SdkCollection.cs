// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Construction;

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild
{
    public class SdkCollection : ICollection<string>
    {
        private readonly ProjectRootElement _projectRoot;

        public SdkCollection(ProjectRootElement projectRoot)
        {
            _projectRoot = projectRoot;
        }

        private HashSet<string> GetSdks() => _projectRoot.Sdk.Split(';').ToHashSet(StringComparer.OrdinalIgnoreCase);

        private ICollection<string> GetSdkLists() => _projectRoot.Sdk.Split(';').ToList();

        public bool Contains(string item)
        {
            return GetSdks().Contains(item);
        }

        public void Add(string item)
        {
            if (!Contains(item))
            {
                _projectRoot.Sdk = string.Concat(_projectRoot.Sdk, ";", item);
            }
        }

        public void Clear()
        {
            _projectRoot.Sdk = string.Empty;
        }

        public int Count
        {
            get
            {
                return GetSdks().Count;
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(string item)
        {
            if (Contains(item))
            {
                var sdkList = GetSdkLists();
                sdkList.Remove(item);
                _projectRoot.Sdk = string.Join(";", sdkList);
                return true;
            }

            return false;
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
            ((ICollection<string>)this).CopyTo(array, arrayIndex);
        }

        public IEnumerator<string> GetEnumerator() => GetSdkLists().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
