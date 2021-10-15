// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

using VerifyCS = Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Test.CSharpCodeFixVerifier<
     Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.AdapterRefactorAnalyzer,
     Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CodeFixes.AdapterCallFactoryCodeFixer>;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Test.AbstractionRefactor
{
    public class CSharpAdapterCallFactoryCodeFixerTests : AdapterTestBase
    {
        [Fact]
        public async Task ArgumentToMethod()
        {
            var refactor = new AdapterDescriptorFactory("RefactorTest", "SomeClass", "ISome");
            var testFile = @"
[assembly: Microsoft.CodeAnalysis.Refactoring.AdapterFactoryDescriptor(typeof(RefactorTest.TestFactory), nameof(RefactorTest.TestFactory.Create))]

namespace RefactorTest
{
    public class Test
    {
        public void Run({|#0:SomeClass|} c) => Method({|#1:c|});

        public void Method(ISome s)
        {
        }
    }

    public class SomeClass
    {
        public void SomeMethod()
        {
        }
    }

    public interface ISome
    {
    }

    public static class TestFactory
    {
      public static ISome Create(SomeClass s)
      {
        s.SomeMethod();
        return null;
      }
    }
}";

            const string withFix = @"
[assembly: Microsoft.CodeAnalysis.Refactoring.AdapterFactoryDescriptor(typeof(RefactorTest.TestFactory), nameof(RefactorTest.TestFactory.Create))]

namespace RefactorTest
{
    public class Test
    {
        public void Run({|#0:SomeClass|} c) => Method(TestFactory.Create(c));

        public void Method(ISome s)
        {
        }
    }

    public class SomeClass
    {
        public void SomeMethod()
        {
        }
    }

    public interface ISome
    {
    }

    public static class TestFactory
    {
      public static ISome Create(SomeClass s)
      {
        s.SomeMethod();
        return null;
      }
    }
}";

            var diagnostic1 = VerifyCS.Diagnostic(AdapterRefactorAnalyzer.RefactorDiagnosticId).WithLocation(0).WithArguments(refactor.Original, refactor.Destination);
            var diagnostic2 = VerifyCS.Diagnostic(AdapterRefactorAnalyzer.CallFactoryDiagnosticId).WithLocation(1).WithArguments(refactor.Original, refactor.Destination);
            var diagnostic3 = DiagnosticResult.CompilerError("CS1503").WithLocation(1).WithArguments("1", "RefactorTest.SomeClass", "RefactorTest.ISome");

            await CreateTest(VerifyCS.Create(), refactor)
                .WithSource(testFile)
                .WithExpectedDiagnostics(diagnostic1, diagnostic2, diagnostic3)
                .WithFixed(withFix)
                .WithExpectedDiagnosticsAfter(diagnostic1)
                .RunAsync();
        }

        [Fact]
        public async Task LocalVariableCallFactory()
        {
            var refactor = new AdapterDescriptorFactory("RefactorTest", "SomeClass", "ISome");
            var testFile = @"
[assembly: Microsoft.CodeAnalysis.Refactoring.AdapterFactoryDescriptor(typeof(RefactorTest.TestFactory), nameof(RefactorTest.TestFactory.Create))]

namespace RefactorTest
{
    public class Test
    {
        public void Run({|#0:SomeClass|} c)
        {
            var other = c;
            Method({|#1:other|});
        }

        public void Method(ISome s)
        {
        }
    }

    public class SomeClass
    {
        public void SomeMethod()
        {
        }
    }

    public interface ISome
    {
    }

    public static class TestFactory
    {
      public static ISome Create(SomeClass s)
      {
        s.SomeMethod();
        return null;
      }
    }
}";

            const string withFix = @"
[assembly: Microsoft.CodeAnalysis.Refactoring.AdapterFactoryDescriptor(typeof(RefactorTest.TestFactory), nameof(RefactorTest.TestFactory.Create))]

namespace RefactorTest
{
    public class Test
    {
        public void Run({|#0:SomeClass|} c)
        {
            var other = c;
            Method(TestFactory.Create(other));
        }

        public void Method(ISome s)
        {
        }
    }

    public class SomeClass
    {
        public void SomeMethod()
        {
        }
    }

    public interface ISome
    {
    }

    public static class TestFactory
    {
      public static ISome Create(SomeClass s)
      {
        s.SomeMethod();
        return null;
      }
    }
}";

            var diagnostic1 = VerifyCS.Diagnostic(AdapterRefactorAnalyzer.RefactorDiagnosticId).WithLocation(0).WithArguments(refactor.Original, refactor.Destination);
            var diagnostic2 = VerifyCS.Diagnostic(AdapterRefactorAnalyzer.CallFactoryDiagnosticId).WithLocation(1).WithArguments(refactor.Original, refactor.Destination);
            var diagnostic3 = DiagnosticResult.CompilerError("CS1503").WithLocation(1).WithArguments("1", "RefactorTest.SomeClass", "RefactorTest.ISome");

            await CreateTest(VerifyCS.Create(), refactor)
                .WithSource(testFile)
                .WithExpectedDiagnostics(diagnostic1, diagnostic2, diagnostic3)
                .WithFixed(withFix)
                .WithExpectedDiagnosticsAfter(diagnostic1)
                .RunAsync();
        }

        [Fact]
        public async Task ReturnTypeChanges()
        {
            var refactor = new AdapterDescriptorFactory("RefactorTest", "SomeClass", "ISome");
            var testFile = @"
[assembly: Microsoft.CodeAnalysis.Refactoring.AdapterFactoryDescriptor(typeof(RefactorTest.TestFactory), nameof(RefactorTest.TestFactory.Create))]

namespace RefactorTest
{
    public class Test
    {
        public ISome Run() => {|#0:new SomeClass()|};
    }

    public class SomeClass
    {
    }

    public interface ISome
    {
    }

    public static class TestFactory
    {
      public static ISome Create(SomeClass s) => null;
    }
}";

            const string withFix = @"
[assembly: Microsoft.CodeAnalysis.Refactoring.AdapterFactoryDescriptor(typeof(RefactorTest.TestFactory), nameof(RefactorTest.TestFactory.Create))]

namespace RefactorTest
{
    public class Test
    {
        public ISome Run() => TestFactory.Create(new SomeClass());
    }

    public class SomeClass
    {
    }

    public interface ISome
    {
    }

    public static class TestFactory
    {
      public static ISome Create(SomeClass s) => null;
    }
}";

            var diagnostic1 = VerifyCS.Diagnostic(AdapterRefactorAnalyzer.CallFactoryDiagnosticId).WithLocation(0).WithArguments(refactor.Original, refactor.Destination);
            var diagnostic2 = DiagnosticResult.CompilerError("CS0266").WithLocation(0).WithArguments("RefactorTest.SomeClass", "RefactorTest.ISome");

            await CreateTest(VerifyCS.Create(), refactor)
                .WithExpectedDiagnostics(diagnostic1, diagnostic2)
                .WithSource(testFile)
                .WithFixed(withFix)
                .RunAsync();
        }
    }
}
