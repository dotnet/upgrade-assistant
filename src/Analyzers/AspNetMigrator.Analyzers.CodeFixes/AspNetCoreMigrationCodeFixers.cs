using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeFixes;

namespace AspNetMigrator.Analyzers
{
    public static class AspNetCoreMigrationCodeFixers
    {
        public static ImmutableArray<CodeFixProvider> AllCodeFixProviders => ImmutableArray.Create<CodeFixProvider>(
            new UsingSystemWebCodeFixer(),
            new HtmlStringCodeFixer(),
            new ResultTypeCodeFixer(),
            new FilterCodeFixer(),
            new HttpContextCurrentCodeFixer());
    }
}
