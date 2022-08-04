// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;

using Microsoft.Build.Evaluation;

namespace MSBuild.Abstractions
{
    public class MSBuildProjectItem : IProjectItem
    {
        private readonly ProjectItem _item;

        public MSBuildProjectItem(ProjectItem item)
        {
            _item = item;
        }

        public string ItemType => _item.ItemType;

        public string EvaluatedInclude => _item.EvaluatedInclude;

        public IEnumerable<IProjectMetadata> DirectMetadata => _item.DirectMetadata.Select(md => new MSBuildProjectMetadata(md));
    }
}
