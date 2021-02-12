using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.UpgradeAssistant.Reporting
{
    public record Table : Content
    {
        public IReadOnlyCollection<string> Headers { get; init; } = Array.Empty<string>();

        public IReadOnlyCollection<Row> Rows { get; init; } = Array.Empty<Row>();

        public IReadOnlyCollection<double> ColumnWidth { get; init; } = Array.Empty<double>();
    }
}
