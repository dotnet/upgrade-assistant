using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.CodeAnalysis;

namespace MSBuild.Abstractions
{
    public class ProjectItemComparer : IEqualityComparer<IProjectItem>
    {
        private readonly bool _compareMetadata;
        
        public static ProjectItemComparer IncludeComparer = new ProjectItemComparer(compareMetadata: false);
        public static ProjectItemComparer MetadataComparer = new ProjectItemComparer(compareMetadata: true);

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
