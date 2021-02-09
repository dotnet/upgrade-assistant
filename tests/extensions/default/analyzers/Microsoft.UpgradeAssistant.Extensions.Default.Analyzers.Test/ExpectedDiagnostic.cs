using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace AspNetMigrator.Analyzers.Test
{
    public class ExpectedDiagnostic
    {
        public string Id { get; }

        public TextSpan SourceSpan { get; }

        public ExpectedDiagnostic(string id, TextSpan sourceSpan)
        {
            Id = id;
            SourceSpan = sourceSpan;
        }

        public bool Matches(Diagnostic diagnostic) => (diagnostic?.Id.Equals(Id, StringComparison.Ordinal) ?? false) && diagnostic.Location.SourceSpan.Equals(SourceSpan);
    }
}
