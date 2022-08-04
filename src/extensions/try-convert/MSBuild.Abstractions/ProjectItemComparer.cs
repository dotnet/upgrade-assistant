// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace MSBuild.Abstractions
{
    public class ProjectItemComparer : IEqualityComparer<IProjectItem>
    {
        private readonly bool _compareMetadata;

#pragma warning disable SA1401 // Fields should be private - this enables sugar like ProjectItemComparer.IncludeComparer
        public static ProjectItemComparer IncludeComparer = new ProjectItemComparer(compareMetadata: false);
        public static ProjectItemComparer MetadataComparer = new ProjectItemComparer(compareMetadata: true);
#pragma warning restore SA1401 // Fields should be private

        private ProjectItemComparer(bool compareMetadata)
        {
            _compareMetadata = compareMetadata;
        }

        public bool Equals([AllowNull] IProjectItem x, [AllowNull] IProjectItem y)
        {
            if (x == null && y == null)
            {
                return true;
            }

            if (x == null)
            {
                return false;
            }

            if (y == null)
            {
                return false;
            }

            // If y has all the metadata that x has then we declare them as equal. This is because
            // the sdk can add new metadata but there's not reason to remove them during conversion.
            var metadataEqual = _compareMetadata ?
                                 x.DirectMetadata.All(xmd => y.DirectMetadata.Any(
                                     ymd => xmd.Name.Equals(ymd.Name, System.StringComparison.OrdinalIgnoreCase) &&
                                            xmd.EvaluatedValue.Equals(ymd.EvaluatedValue, System.StringComparison.OrdinalIgnoreCase)))
                                 : true;

            return x.ItemType == y.ItemType && x.EvaluatedInclude.Equals(y.EvaluatedInclude, System.StringComparison.OrdinalIgnoreCase) && metadataEqual;
        }

        public int GetHashCode(IProjectItem obj)
        {
            return (obj.EvaluatedInclude.ToLowerInvariant() + obj.ItemType).GetHashCode();
        }
    }
}
