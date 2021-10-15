// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

using VerifyCS = Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Test.CSharpCodeFixVerifier<
     Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.AdapterRefactorAnalyzer,
     Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CodeFixes.AdapterRefactorCodeFixer>;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Test
{
    public class CSharpAbstractionRefactorTests : AdapterTestBase
    {
        [Fact]
        public async Task EmptyCode()
        {
            var testFile = string.Empty;

            await VerifyCS.Create().WithSource(testFile).RunAsync();
        }

        [Fact]
        public async Task JustDefinitionsNothingElse()
        {
            var refactor = new AdapterDescriptorFactory("RefactorTest", "SomeClass", "ISome");
            var testFile = @"
namespace RefactorTest
{
    public class SomeClass
    {
    }

    public interface ISome
    {
    }
}";

            await CreateTest(refactor, withFix: false)
                .WithSource(testFile)
                .RunAsync();
        }

        [Fact]
        public async Task SingleChange()
        {
            var refactor = new AdapterDescriptorFactory("RefactorTest", "SomeClass", "ISome");
            var testFile = @"
namespace RefactorTest
{
    public static class Test
    {
        public static void Helper({|#0:SomeClass|} c)
        {
        }
    }

    public class SomeClass
    {
    }

    public interface ISome
    {
    }
}";

            const string withFix = @"
namespace RefactorTest
{
    public static class Test
    {
        public static void Helper(ISome c)
        {
        }
    }

    public class SomeClass
    {
    }

    public interface ISome
    {
    }
}";

            var diagnostic = VerifyCS.Diagnostic(AdapterRefactorAnalyzer.RefactorDiagnosticId).WithLocation(0).WithArguments(refactor.Original, refactor.Destination);
            await CreateTest(refactor)
                .WithSource(testFile)
                .WithExpectedDiagnostics(diagnostic)
                .WithFixed(withFix)
                .RunAsync();
        }

        [Fact]
        public async Task MethodReturn()
        {
            var refactor = new AdapterDescriptorFactory("RefactorTest", "SomeClass", "ISome");
            var testFile = @"
namespace RefactorTest
{
    public static class Test
    {
        public static {|#0:SomeClass|} Helper() => null;
    }

    public class SomeClass
    {
    }

    public interface ISome
    {
    }
}";

            const string withFix = @"
namespace RefactorTest
{
    public static class Test
    {
        public static ISome Helper() => null;
    }

    public class SomeClass
    {
    }

    public interface ISome
    {
    }
}";

            var diagnostic = VerifyCS.Diagnostic(AdapterRefactorAnalyzer.RefactorDiagnosticId).WithLocation(0).WithArguments(refactor.Original, refactor.Destination);
            await CreateTest(refactor)
                .WithSource(testFile)
                .WithExpectedDiagnostics(diagnostic)
                .WithFixed(withFix)
                .RunAsync();
        }

        [Fact]
        public async Task Field()
        {
            var refactor = new AdapterDescriptorFactory("RefactorTest", "SomeClass", "ISome");
            var testFile = @"
namespace RefactorTest
{
    public class Test
    {
        private {|#0:SomeClass|} _instance;
    }

    public class SomeClass
    {
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
        private ISome _instance;
    }

    public class SomeClass
    {
    }

    public interface ISome
    {
    }
}";

            var diagnostic = VerifyCS.Diagnostic(AdapterRefactorAnalyzer.RefactorDiagnosticId).WithLocation(0).WithArguments(refactor.Original, refactor.Destination);
            await CreateTest(refactor)
                .WithSource(testFile)
                .WithExpectedDiagnostics(diagnostic)
                .WithFixed(withFix)
                .RunAsync();
        }

        [Fact]
        public async Task Property()
        {
            var refactor = new AdapterDescriptorFactory("RefactorTest", "SomeClass", "ISome");
            var testFile = @"
namespace RefactorTest
{
    public class Test
    {
        public {|#0:SomeClass|} Instance { get; set; }
    }

    public class SomeClass
    {
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
        public ISome Instance { get; set; }
    }

    public class SomeClass
    {
    }

    public interface ISome
    {
    }
}";

            var diagnostic = VerifyCS.Diagnostic(AdapterRefactorAnalyzer.RefactorDiagnosticId).WithLocation(0).WithArguments(refactor.Original, refactor.Destination);
            await CreateTest(refactor)
                .WithSource(testFile)
                .WithExpectedDiagnostics(diagnostic)
                .WithFixed(withFix)
                .RunAsync();
        }

        [Fact]
        public async Task ChangeTwice()
        {
            var refactor = new AdapterDescriptorFactory("RefactorTest", "SomeClass", "ISome");
            var testFile = @"
namespace RefactorTest
{
    public static class Test
    {
        public static void Helper({|#0:SomeClass|} c)
        {
        }

        public static void Helper2({|#1:SomeClass|} c)
            => Helper(c);
    }

    public class SomeClass
    {
    }

    public interface ISome
    {
    }
}";

            const string withFix = @"
namespace RefactorTest
{
    public static class Test
    {
        public static void Helper(ISome c)
        {
        }

        public static void Helper2(ISome c)
            => Helper(c);
    }

    public class SomeClass
    {
    }

    public interface ISome
    {
    }
}";

            var diagnostic1 = VerifyCS.Diagnostic(AdapterRefactorAnalyzer.RefactorDiagnosticId).WithLocation(0).WithArguments(refactor.Original, refactor.Destination);
            var diagnostic2 = VerifyCS.Diagnostic(AdapterRefactorAnalyzer.RefactorDiagnosticId).WithLocation(1).WithArguments(refactor.Original, refactor.Destination);
            await CreateTest(refactor)
                .WithSource(testFile)
                .WithExpectedDiagnostics(diagnostic1, diagnostic2)
                .WithFixed(withFix)
                .RunAsync();
        }

        private static ICodeFixTest CreateTest(AdapterDescriptorFactory? attributeDescriptor = null, bool withFix = true)
            => CreateTest(VerifyCS.Create(), attributeDescriptor, withFix);
    }
}
