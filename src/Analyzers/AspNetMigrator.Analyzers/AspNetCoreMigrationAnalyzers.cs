using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AspNetMigrator.Analyzers
{
    public static class AspNetCoreMigrationAnalyzers
    {
        public static ImmutableArray<DiagnosticAnalyzer> AllAnalyzers => ImmutableArray.Create<DiagnosticAnalyzer>(
            new UsingSystemWebAnalyzer(),
            new HtmlStringAnalyzer(),
            new ResultTypeAnalyzer(),
            new FilterAnalyzer(),
            new HttpContextCurrentAnalyzer(),
            new HttpContextIsDebuggingEnabledAnalyzer());
    }
}
