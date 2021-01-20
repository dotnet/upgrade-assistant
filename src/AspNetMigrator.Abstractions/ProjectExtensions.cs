using System;

namespace AspNetMigrator
{
    public static class ProjectExtensions
    {
        public static IProject Required(this IProject? project)
            => project ?? throw new InvalidOperationException("Project cannot be null");
    }
}
