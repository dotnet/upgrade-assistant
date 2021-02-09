using System;
using System.Collections.Generic;

namespace AspNetMigrator.Reporting
{
    public record Page(string Title)
    {
        public IReadOnlyCollection<Content> Content { get; init; } = Array.Empty<Content>();
    }
}
