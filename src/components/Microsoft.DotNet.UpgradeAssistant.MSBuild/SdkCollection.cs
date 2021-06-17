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

        public bool Contains(string item)
        {
            var sdkElements = _projectRoot.Sdk.Split(';').ToList();
            if (sdkElements.Contains(item))
            {
                return true;
            }

            return false;
        }

        public void Add(string item)
        {
            var sdkElements = _projectRoot.Sdk.Split(';').ToList();
            if (!Contains(item))
            {
                sdkElements.Add(item);
                _projectRoot.Sdk = string.Join(';', sdkElements);
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
                return _projectRoot.Imports.Count;
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(string item)
        {
            var sdkElements = _projectRoot.Sdk.Split(';').ToList();
            if (Contains(item))
            {
                sdkElements.Remove(item);
                _projectRoot.Sdk = string.Join(';', sdkElements);
                return true;
            }

            return false;
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
            ((ICollection<string>)this).CopyTo(array, arrayIndex);
        }

        public IEnumerator<string> GetEnumerator()
        {
            return _projectRoot.Sdk.Split(';').ToList().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _projectRoot.Sdk.Split(';').ToArray().GetEnumerator();
        }
    }
}
