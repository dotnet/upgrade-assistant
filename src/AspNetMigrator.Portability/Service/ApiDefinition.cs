using System;

namespace AspNetMigrator.Portability.Service
{
    public class ApiDefinition
    {
        public string DocId { get; set; } = string.Empty;

        public string ReturnType { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string Parent { get; set; } = string.Empty;

        public override bool Equals(object? obj)
        {
            if (obj is not ApiDefinition other)
            {
                return false;
            }

            return string.Equals(DocId, other.DocId, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            var hashcode = default(HashCode);
            hashcode.Add(DocId, StringComparer.Ordinal);
            return hashcode.ToHashCode();
        }
    }
}
