// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

using VerifyCS = Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Test.CSharpCodeFixVerifier<
     Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.AdapterRefactorAnalyzer,
     Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CodeFixes.AdapterStaticMemberCodeFixer>;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Test
{
    public class CSharpAdapterStaticMemberCodeFixerTests : AdapterTestBase
    {
        [Fact]
        public async Task SingleChange()
        {
            var testFile = @"
[assembly: Microsoft.CodeAnalysis.Refactoring.AdapterStaticDescriptor(typeof(RefactorTest.SomeClass), nameof(RefactorTest.SomeClass.Prop1), typeof(RefactorTest.OtherClass), nameof(RefactorTest.OtherClass.Prop2))]

namespace RefactorTest
{
    public static class Test
    {
        public static bool Helper() => {|#0:SomeClass.Prop1|};
    }

    public static class SomeClass
    {
        public static bool Prop1 { get; }
    }

    public static class OtherClass
    {
        public static bool Prop2 { get; }
    }
}";

            const string withFix = @"
[assembly: Microsoft.CodeAnalysis.Refactoring.AdapterStaticDescriptor(typeof(RefactorTest.SomeClass), nameof(RefactorTest.SomeClass.Prop1), typeof(RefactorTest.OtherClass), nameof(RefactorTest.OtherClass.Prop2))]

namespace RefactorTest
{
    public static class Test
    {
        public static bool Helper() => OtherClass.Prop2;
    }

    public static class SomeClass
    {
        public static bool Prop1 { get; }
    }

    public static class OtherClass
    {
        public static bool Prop2 { get; }
    }
}";

            var diagnostic = VerifyCS.Diagnostic(AdapterRefactorAnalyzer.StaticMemberDiagnosticId).WithLocation(0).WithArguments("RefactorTest.SomeClass.Prop1", "RefactorTest.OtherClass.Prop2");

            await CreateTest()
                .WithSource(testFile)
                .WithExpectedDiagnostics(diagnostic)
                .WithFixed(withFix)
                .RunAsync();
        }

        private static ICodeFixTest CreateTest(AdapterDescriptorFactory? attributeDescriptor = null, bool withFix = true)
            => CreateTest(VerifyCS.Create(), attributeDescriptor, withFix);
    }
}
