// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

using VerifyCS = Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Test.CSharpCodeFixVerifier<
     Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.HttpContextCurrentAnalyzer,
     Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CodeFixes.HttpContextRefactorCodeFixProvider>;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Test
{
    public class CSharpHttpContextRefactorCodeFixProviderTests
    {
        private const string HttpContextName = "HttpContext";
        private const string HttpContextCurrentName = "HttpContext HttpContext.Current";
        private const string DiagnosticId = HttpContextCurrentAnalyzer.DiagnosticId;

        [Fact]
        public async Task EmptyCode()
        {
            var testFile = string.Empty;

            await VerifyCS.Create().WithSource(testFile).RunAsync();
        }

        [Fact]
        public async Task SimpleUse()
        {
            var testFile = @"
    using System.Web;

    namespace ConsoleApplication1
    {
        class Program
        {
            public void Test()
            {
                _ = {|#0:HttpContext.Current|};
            }
        }
    }";
            var fixedFile = @"
    using System.Web;

    namespace ConsoleApplication1
    {
        class Program
        {
            public void Test(HttpContext currentContext)
            {
                _ = currentContext;
            }
        }
    }";

            var expected = VerifyCS.Diagnostic(DiagnosticId).WithLocation(0).WithArguments(HttpContextName, HttpContextCurrentName);
            await CreateTest().WithSource(testFile).WithFixed(fixedFile).WithExpectedDiagnostics(expected).RunAsync();
        }

        [Fact]
        public async Task ExpressionBody()
        {
            var testFile = @"
    using System.Web;

    namespace ConsoleApplication1
    {
        class Program
        {
            public object Test() => {|#0:HttpContext.Current|};
        }
    }";
            var fixedFile = @"
    using System.Web;

    namespace ConsoleApplication1
    {
        class Program
        {
            public object Test(HttpContext currentContext) => currentContext;
        }
    }";

            var expected = VerifyCS.Diagnostic(DiagnosticId).WithLocation(0).WithArguments(HttpContextName, HttpContextCurrentName);
            await CreateTest().WithSource(testFile).WithFixed(fixedFile).WithExpectedDiagnostics(expected).RunAsync();
        }

        [Fact]
        public async Task ReuseArgument()
        {
            var testFile = @"
    using System.Web;

    namespace ConsoleApplication1
    {
        class Program
        {
            public void Test(HttpContext currentContext)
            {
                _ = {|#0:HttpContext.Current|};
            }
        }
    }";
            var fixedFile = @"
    using System.Web;

    namespace ConsoleApplication1
    {
        class Program
        {
            public void Test(HttpContext currentContext)
            {
                _ = currentContext;
            }
        }
    }";

            var expected = VerifyCS.Diagnostic(DiagnosticId).WithLocation(0).WithArguments(HttpContextName, HttpContextCurrentName);
            await CreateTest().WithSource(testFile).WithFixed(fixedFile).WithExpectedDiagnostics(expected).RunAsync();
        }

        [Fact]
        public async Task SuffixNameIfAlreadyExists()
        {
            var testFile = @"
    using System.Web;

    namespace ConsoleApplication1
    {
        class Program
        {
            public void Test(object currentContext)
            {
                _ = {|#0:HttpContext.Current|};
            }
        }
    }";
            var fixedFile = @"
    using System.Web;

    namespace ConsoleApplication1
    {
        class Program
        {
            public void Test(object currentContext, HttpContext currentContext2)
            {
                _ = currentContext2;
            }
        }
    }";

            var expected = VerifyCS.Diagnostic(DiagnosticId).WithLocation(0).WithArguments(HttpContextName, HttpContextCurrentName);
            await CreateTest().WithSource(testFile).WithFixed(fixedFile).WithExpectedDiagnostics(expected).RunAsync();
        }

        [Fact]
        public async Task SuffixNameTwiceIfAlreadyExists()
        {
            var testFile = @"
    using System.Web;

    namespace ConsoleApplication1
    {
        class Program
        {
            public void Test(object currentContext, object currentContext2)
            {
                _ = {|#0:HttpContext.Current|};
            }
        }
    }";
            var fixedFile = @"
    using System.Web;

    namespace ConsoleApplication1
    {
        class Program
        {
            public void Test(object currentContext, object currentContext2, HttpContext currentContext3)
            {
                _ = currentContext3;
            }
        }
    }";

            var expected = VerifyCS.Diagnostic(DiagnosticId).WithLocation(0).WithArguments(HttpContextName, HttpContextCurrentName);
            await CreateTest().WithSource(testFile).WithFixed(fixedFile).WithExpectedDiagnostics(expected).RunAsync();
        }

        [Fact]
        public async Task ReuseProperty()
        {
            var testFile = @"
    using System.Web;

    namespace ConsoleApplication1
    {
        class Program
        {
            public HttpContext SomeContext => null;

            public void Test()
            {
                _ = {|#0:HttpContext.Current|};
            }
        }
    }";
            var fixedFile = @"
    using System.Web;

    namespace ConsoleApplication1
    {
        class Program
        {
            public HttpContext SomeContext => null;

            public void Test()
            {
                _ = SomeContext;
            }
        }
    }";

            var expected = VerifyCS.Diagnostic(DiagnosticId).WithLocation(0).WithArguments(HttpContextName, HttpContextCurrentName);
            await CreateTest().WithSource(testFile).WithFixed(fixedFile).WithExpectedDiagnostics(expected).RunAsync();
        }

        [Fact]
        public async Task ReuseArgumentNotProperty()
        {
            var testFile = @"
    using System.Web;

    namespace ConsoleApplication1
    {
        class Program
        {
            public HttpContext SomeContext => null;

            public void Test(HttpContext currentContext)
            {
                _ = {|#0:HttpContext.Current|};
            }
        }
    }";
            var fixedFile = @"
    using System.Web;

    namespace ConsoleApplication1
    {
        class Program
        {
            public HttpContext SomeContext => null;

            public void Test(HttpContext currentContext)
            {
                _ = currentContext;
            }
        }
    }";

            var expected = VerifyCS.Diagnostic(DiagnosticId).WithLocation(0).WithArguments(HttpContextName, HttpContextCurrentName);
            await CreateTest().WithSource(testFile).WithFixed(fixedFile).WithExpectedDiagnostics(expected).RunAsync();
        }

        [Fact]
        public async Task InArgument()
        {
            var testFile = @"
    using System.Web;

    namespace ConsoleApp1
    {
        public class Program
        {
            private static void Test(HttpContext currentContext)
            {
            }
            public static void Test2() => Test({|#0:HttpContext.Current|});
        }
    }";
            var fixedFile = @"
    using System.Web;

    namespace ConsoleApp1
    {
        public class Program
        {
            private static void Test(HttpContext currentContext)
            {
            }
            public static void Test2(HttpContext currentContext) => Test(currentContext);
        }
    }";

            var expected = VerifyCS.Diagnostic(DiagnosticId).WithLocation(0).WithArguments(HttpContextName, HttpContextCurrentName);
            await CreateTest().WithSource(testFile).WithFixed(fixedFile).WithExpectedDiagnostics(expected).RunAsync();
        }

        [Fact]
        public async Task ReuseParameterName()
        {
            var testFile = @"
    using System.Web;

    namespace ConsoleApp1
    {
        public class Program
        {
            private static void Test()
            {
                Test2({|#0:HttpContext.Current|});
            }
            public static void Test2(HttpContext currentContext) => Test();
        }
    }";
            var fixedFile = @"
    using System.Web;

    namespace ConsoleApp1
    {
        public class Program
        {
            private static void Test(HttpContext currentContext)
            {
                Test2(currentContext);
            }
            public static void Test2(HttpContext currentContext) => Test(currentContext);
        }
    }";

            var expected = VerifyCS.Diagnostic(DiagnosticId).WithLocation(0).WithArguments(HttpContextName, HttpContextCurrentName);
            await CreateTest().WithSource(testFile).WithFixed(fixedFile).WithExpectedDiagnostics(expected).RunAsync();
        }

        [Fact]
        public async Task ReplaceCallerInSameDocument()
        {
            var testFile = @"
    using System.Web;

    namespace ConsoleApplication1
    {
        class Program
        {
            public void Test()
            {
                _ = {|#0:HttpContext.Current|};
            }

            public void Test2()
            {
                Test();
            }
        }
    }";
            var fixedFile = @"
    using System.Web;

    namespace ConsoleApplication1
    {
        class Program
        {
            public void Test(HttpContext currentContext)
            {
                _ = currentContext;
            }

            public void Test2()
            {
                Test({|#0:HttpContext.Current|});
            }
        }
    }";

            var expected1 = VerifyCS.Diagnostic(DiagnosticId).WithLocation(0).WithArguments(HttpContextName, HttpContextCurrentName);
            var expected2 = VerifyCS.Diagnostic(DiagnosticId).WithLocation(0).WithArguments(HttpContextName, HttpContextCurrentName);

            await CreateTest().WithSource(testFile).WithFixed(fixedFile)
                .WithExpectedDiagnostics(expected1)
                .WithExpectedDiagnosticsAfter(expected2)
                .RunAsync();
        }

        [Fact]
        public async Task InProperty()
        {
            var testFile = @"
    using System.Web;

    namespace ConsoleApplication1
    {
        class Program
        {
            public object Instance => {|#0:HttpContext.Current|};
        }
    }";

            var expected1 = VerifyCS.Diagnostic(DiagnosticId).WithLocation(0).WithArguments(HttpContextName, HttpContextCurrentName);

            await CreateTest().WithSource(testFile).WithFixed(testFile)
                .WithExpectedDiagnostics(expected1)
                .WithExpectedDiagnosticsAfter(expected1)
                .RunAsync();
        }

        [Fact]
        public async Task MultipleFiles()
        {
            var testFile1 = @"
    using System.Web;

    namespace ConsoleApplication1
    {
        public class Program
        {
            public static object Instance() => {|#0:HttpContext.Current|};
        }
    }";
            var testFile2 = @"
    using System.Web;

    namespace ConsoleApplication1
    {
        class Program2
        {
            public object Instance() => Program.Instance();
        }
    }";

            var fixedFile1 = @"
    using System.Web;

    namespace ConsoleApplication1
    {
        public class Program
        {
            public static object Instance(HttpContext currentContext) => currentContext;
        }
    }";
            var fixedFile2 = @"
    using System.Web;

    namespace ConsoleApplication1
    {
        class Program2
        {
            public object Instance() => Program.Instance({|#0:HttpContext.Current|});
        }
    }";

            var expected = VerifyCS.Diagnostic(DiagnosticId).WithLocation(0).WithArguments(HttpContextName, HttpContextCurrentName);

            await CreateTest()
                .WithSource(testFile1)
                .WithSource(testFile2)
                .WithFixed(fixedFile1)
                .WithFixed(fixedFile2)
                .WithExpectedDiagnostics(expected)
                .WithExpectedDiagnosticsAfter(expected)
                .RunAsync();
        }

        [Fact]
        public async Task MultipleFilesNoSystemWebUsing()
        {
            var test1 = @"
    using System.Web;

    namespace ConsoleApplication1
    {
        public class Program
        {
            public static object Instance() => {|#0:HttpContext.Current|};
        }
    }";
            var test2 = @"
    namespace ConsoleApplication1
    {
        class Program2
        {
            public object Instance() => Program.Instance();
        }
    }";

            var fix1 = @"
    using System.Web;

    namespace ConsoleApplication1
    {
        public class Program
        {
            public static object Instance(HttpContext currentContext) => currentContext;
        }
    }";
            var fix2 = @"
    namespace ConsoleApplication1
    {
        class Program2
        {
            public object Instance() => Program.Instance({|#0:System.Web.HttpContext.Current|});
        }
    }";
            var expected = VerifyCS.Diagnostic(DiagnosticId).WithLocation(0).WithArguments(HttpContextName, HttpContextCurrentName);

            await CreateTest()
                .WithSource(test1)
                .WithSource(test2)
                .WithFixed(fix1)
                .WithFixed(fix2)
                .With(CodeActionValidationMode.None)
                .WithExpectedDiagnostics(expected)
                .WithExpectedDiagnosticsAfter(expected)
                .RunAsync();
        }

        private static VerifyCS.Test CreateTest()
            => VerifyCS.Create()
                .WithSystemWeb()
                .With(CodeFixTestBehaviors.FixOne);
    }
}
