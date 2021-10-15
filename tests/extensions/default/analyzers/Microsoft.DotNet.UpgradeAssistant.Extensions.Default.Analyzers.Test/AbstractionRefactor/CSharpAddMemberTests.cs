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
        public async Task WithMethodAccess()
        {
            var refactor = new AdapterDescriptorFactory("RefactorTest", "SomeClass", "ISome");
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

        [Fact]
        public async Task IgnoreStaticMethods()
        {
            var refactor = new AdapterDescriptorFactory("RefactorTest", "SomeClass", "ISome");
            var testFile = @"
namespace RefactorTest
{
    public class Test
    {
        public void Run()
        {
            var isValid = SomeClass.IsValid();
        }
    }

    public class SomeClass
    {
       public static bool IsValid() => true;
    }

    public interface ISome
    {
    }
}";

            await CreateTest(VerifyCSAddMember.Create(), refactor, withFix: false)
                .WithSource(testFile)
                .RunAsync();
        }

        [Fact]
        public async Task WithPropertyAccess()
        {
            var refactor = new AdapterDescriptorFactory("RefactorTest", "SomeClass", "ISome");
            var testFile = @"
namespace RefactorTest
{
    public class Test
    {
        public void Run({|#0:SomeClass|} c)
        {
            var isValid = {|#1:c.IsValid|};
        }
    }

    public class SomeClass
    {
       public bool IsValid { get; }
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
            var isValid = {|#1:c.IsValid|};
        }
    }

    public class SomeClass
    {
       public bool IsValid { get; }
    }

    public interface ISome
    {
        bool IsValid { get; }
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

        [Fact]
        public async Task WithMemberAccessUsingConcreteTypeAsReturn()
        {
            var refactor = new AdapterDescriptorFactory("RefactorTest", "SomeClass", "ISome");
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

        [Fact]
        public async Task WithMemberAccessUsingConcreteTypeParameter()
        {
            var refactor = new AdapterDescriptorFactory("RefactorTest", "SomeClass", "ISome");
            var testFile = @"
namespace RefactorTest
{
    public class Test
    {
        public void Run({|#0:SomeClass|} c)
        {
            {|#1:c.GetValid(c)|};
        }
    }

    public class SomeClass
    {
       public void GetValid({|#2:SomeClass|} some)
       {
       }
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
            {|#1:c.GetValid(c)|};
        }
    }

    public class SomeClass
    {
       public void GetValid({|#2:SomeClass|} some)
       {
       }
    }

    public interface ISome
    {
        void GetValid(ISome some);
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
