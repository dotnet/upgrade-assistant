// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

using VerifyCSAddMember = Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Test.CSharpCodeFixVerifier<
     Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.AdapterRefactorAnalyzer,
     Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CodeFixes.AdapterAddMemberCodeFixer>;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Test.AbstractionRefactor
{
    public class CSharpAddMemberTests : AdapterTestBase
    {
        [Fact]
        public async Task WithMemberAccess()
        {
            var refactor = new AdapterDescriptor("RefactorTest", "ISome", "SomeClass");
            var testFile = @"
namespace RefactorTest
{
    public class Test
    {
        public void Run({|#0:SomeClass|} c)
        {
            var isValid = {|#1:c.IsValid()|};
        }
    }

    public class SomeClass
    {
       public bool IsValid() => true;
    }

    public interface ISome
    {
    }
}";

            const string withFix = @"
namespace RefactorTest
{
    public class Test
    {
        public void Run({|#0:SomeClass|} c)
        {
            var isValid = {|#1:c.IsValid()|};
        }
    }

    public class SomeClass
    {
       public bool IsValid() => true;
    }

    public interface ISome
    {
        bool IsValid();
    }
}";

            var diagnostic = VerifyCSAddMember.Diagnostic(AdapterRefactorAnalyzer.RefactorDiagnosticId).WithLocation(0).WithArguments(refactor.Original, refactor.Destination);
            var diagnosticFixed = VerifyCSAddMember.Diagnostic(AdapterRefactorAnalyzer.AddMemberDiagnosticId).WithLocation(1).WithArguments("IsValid", $"{refactor.Namespace}.{refactor.Destination}");
            await CreateTest(VerifyCSAddMember.Create(), refactor)
                .WithSource(testFile)
                .WithExpectedDiagnostics(diagnostic, diagnosticFixed)
                .WithFixed(withFix)
                .WithExpectedDiagnosticsAfter(diagnostic)
                .RunAsync();
        }

        [Fact(Skip = "Need to visit all parts of a member to map abstractions")]
        public async Task WithMemberAccessUsingConcreteTypeAsReturn()
        {
            var refactor = new AdapterDescriptor("RefactorTest", "ISome", "SomeClass");
            var testFile = @"
namespace RefactorTest
{
    public class Test
    {
        public void Run({|#0:SomeClass|} c)
        {
            var isValid = {|#1:c.GetValid()|};
        }
    }

    public class SomeClass
    {
       public {|#2:SomeClass|} GetValid() => this;
    }

    public interface ISome
    {
    }
}";

            const string withFix = @"
namespace RefactorTest
{
    public class Test
    {
        public void Run({|#0:SomeClass|} c)
        {
            var isValid = {|#1:c.GetValid()|};
        }
    }

    public class SomeClass
    {
       public {|#2:SomeClass|} GetValid() => this;
    }

    public interface ISome
    {
        ISome GetValid();
    }
}";

            var diagnostic = VerifyCSAddMember.Diagnostic(AdapterRefactorAnalyzer.RefactorDiagnosticId).WithLocation(0).WithArguments(refactor.Original, refactor.Destination);
            var diagnostic2 = VerifyCSAddMember.Diagnostic(AdapterRefactorAnalyzer.RefactorDiagnosticId).WithLocation(2).WithArguments(refactor.Original, refactor.Destination);
            var diagnosticFixed = VerifyCSAddMember.Diagnostic(AdapterRefactorAnalyzer.AddMemberDiagnosticId).WithLocation(1).WithArguments("GetValid", $"{refactor.Namespace}.{refactor.Destination}");
            await CreateTest(VerifyCSAddMember.Create(), refactor)
                .WithSource(testFile)
                .WithExpectedDiagnostics(diagnostic, diagnostic2, diagnosticFixed)
                .WithFixed(withFix)
                .WithExpectedDiagnosticsAfter(diagnostic, diagnostic2)
                .RunAsync();
        }
    }
}
