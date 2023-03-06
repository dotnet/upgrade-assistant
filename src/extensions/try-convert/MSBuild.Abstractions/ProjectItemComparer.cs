// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace MSBuild.Abstractions
{
    public class ProjectItemComparer : IEqualityComparer<IProjectItem>
    {
        private readonly bool _compareMetadata;

#pragma warning disable SA1401 // Fields should be private - this enables sugar like ProjectItemComparer.IncludeComparer
        public static readonly ProjectItemComparer IncludeComparer = new ProjectItemComparer(compareMetadata: false);
        public static readonly ProjectItemComparer MetadataComparer = new ProjectItemComparer(compareMetadata: true);
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

            if (x.ItemType != y.ItemType)
            {
                return false;
            }

            var xPath = PathHelpers.GetIncludePath(x.EvaluatedInclude);
            var yPath = PathHelpers.GetIncludePath(y.EvaluatedInclude);

            if (!xPath.Equals(yPath, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (_compareMetadata)
            {
                // If y has all the metadata that x has then we declare them as equal. This is because
                // the sdk can add new metadata but there's not reason to remove them during conversion.
                var metadataEqual = x.DirectMetadata.All(xmd => y.DirectMetadata.Any(
                                         ymd => xmd.Name.Equals(ymd.Name, StringComparison.OrdinalIgnoreCase) &&
                                                xmd.EvaluatedValue.Equals(ymd.EvaluatedValue, StringComparison.OrdinalIgnoreCase)));

                if (!metadataEqual)
                {
                    return false;
                }
            }

            return true;
        }

        public int GetHashCode(IProjectItem obj)
        {
            obj = obj ?? throw new ArgumentNullException(nameof(obj));

            var path = PathHelpers.GetIncludePath(obj.EvaluatedInclude);

            return (obj.ItemType + ":" + path).GetHashCode(StringComparison.Ordinal);
        }
    }
}
