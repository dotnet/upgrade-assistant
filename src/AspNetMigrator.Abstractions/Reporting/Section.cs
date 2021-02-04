using System;
using System.Collections.Generic;

namespace AspNetMigrator.Reporting
{
    public record Section(string Header)
        : Content
    {
        public IReadOnlyCollection<Content> Content { get; init; } = Array.Empty<Content>();
    }
}
