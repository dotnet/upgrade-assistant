using System;
using System.Collections.Generic;

namespace AspNetMigrator.Reporting
{
    public record Table : Content
    {
        public IReadOnlyCollection<string> Headers { get; set; } = Array.Empty<string>();

        public IReadOnlyCollection<string> Groups { get; set; } = Array.Empty<string>();

        public IReadOnlyCollection<Row> Rows { get; set; } = Array.Empty<Row>();

        public IReadOnlyCollection<double> ColumnWidth { get; init; } = Array.Empty<double>();
    }
}
