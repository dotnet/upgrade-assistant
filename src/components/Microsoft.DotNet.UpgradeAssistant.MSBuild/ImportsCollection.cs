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
    public class ImportsCollection : ICollection<string>
    {
        private readonly ProjectRootElement _projectRoot;

        public ImportsCollection(ProjectRootElement projectRoot)
        {
            _projectRoot = projectRoot;
        }

        public bool Contains(string item)
        {
            if (_projectRoot.Imports.Any(p => item.Equals(p.Project, StringComparison.Ordinal)))
            {
                return true;
            }

            return false;
        }

        public void Add(string item)
        {
            if (!Contains(item))
            {
                _projectRoot.AddImport(item);
            }
        }

        public void Clear()
        {
            _projectRoot.Imports.Clear();
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
            var element = _projectRoot.Imports.FirstOrDefault(p => item.Equals(p.Project, StringComparison.Ordinal));

            if (element != null)
            {
                element.RemoveElement();
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
            return _projectRoot.Imports.Select(p => p.Project).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _projectRoot.Imports.GetEnumerator();
        }
    }
}
