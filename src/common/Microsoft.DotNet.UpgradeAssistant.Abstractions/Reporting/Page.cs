using System;
using System.Collections.Generic;

namespace Microsoft.UpgradeAssistant.Reporting
{
    public record Page(string Title)
    {
        public IReadOnlyCollection<Content> Content { get; init; } = Array.Empty<Content>();
    }
}
