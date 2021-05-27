// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

using VerifyVB = Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Test.VisualBasicCodeFixVerifier<
     Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.HttpContextCurrentAnalyzer,
     Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CodeFixes.HttpContextRefactorCodeFixProvider>;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Test
{
    public class VisualBasicHttpContextRefactorCodeFixProviderTests
    {
        private const string HttpContextName = "HttpContext";
        private const string HttpContextCurrentName = "Property HttpContext.Current As HttpContext";
        private const string DiagnosticId = HttpContextCurrentAnalyzer.DiagnosticId;

        [Fact]
        public async Task EmptyCode()
        {
            var testFile = string.Empty;

            await CreateTest().WithSource(testFile).RunAsync();
        }

        [Fact]
        public async Task SimpleUse()
        {
            var testFile = @"
    Imports System.Web

    Namespace ConsoleApplication1
        Class Program
            Public  Sub Test()
                Dim c = {|#0:HttpContext.Current|}
            End Sub
        End Class
    End Namespace";
            var fixedFile = @"
    Imports System.Web

    Namespace ConsoleApplication1
        Class Program
            Public  Sub Test(currentContext As HttpContext)
                Dim c = currentContext
        End Sub
        End Class
    End Namespace";

            var expected = VerifyVB.Diagnostic(DiagnosticId).WithLocation(0).WithArguments(HttpContextName, HttpContextCurrentName);
            await CreateTest()
                .WithSource(testFile)
                .WithExpectedDiagnostics(expected)
                .WithFixed(fixedFile)
                .RunAsync();
        }

        [Fact]
        public async Task ReuseArgument()
        {
            var testFile = @"
    Imports System.Web

    Namespace ConsoleApplication1
        Class Program
            Public  Sub Test(currentContext As HttpContext)
                Dim c = {|#0:HttpContext.Current|}
            End Sub
        End Class
    End Namespace";
            var fixedFile = @"
    Imports System.Web

    Namespace ConsoleApplication1
        Class Program
            Public  Sub Test(currentContext As HttpContext)
                Dim c = currentContext
        End Sub
        End Class
    End Namespace";

            var expected = VerifyVB.Diagnostic(DiagnosticId).WithLocation(0).WithArguments(HttpContextName, HttpContextCurrentName);

            await CreateTest()
                .WithSource(testFile)
                .WithExpectedDiagnostics(expected)
                .WithFixed(fixedFile)
                .RunAsync();
        }

        [Fact]
        public async Task ReuseProperty()
        {
            var testFile = @"
    Imports System.Web

    Namespace ConsoleApplication1
        Class Program
            Public Property SomeContext As HttpContext
                Get
                    Return Nothing
                End Get
                Set(value As HttpContext)
                End Set
            End Property

            Public  Sub Test()
                Dim c = {|#0:HttpContext.Current|}
            End Sub
        End Class
    End Namespace";
            var fixedFile = @"
    Imports System.Web

    Namespace ConsoleApplication1
        Class Program
            Public Property SomeContext As HttpContext
                Get
                    Return Nothing
                End Get
                Set(value As HttpContext)
                End Set
            End Property

            Public  Sub Test()
                Dim c = SomeContext
        End Sub
        End Class
    End Namespace";

            var expected = VerifyVB.Diagnostic(DiagnosticId).WithLocation(0).WithArguments(HttpContextName, HttpContextCurrentName);
            await CreateTest()
                .WithSource(testFile)
                .WithExpectedDiagnostics(expected)
                .WithFixed(fixedFile)
                .RunAsync();
        }

        [Fact]
        public async Task ReuseArgumentNotProperty()
        {
            var testFile = @"
    Imports System.Web

    Namespace ConsoleApplication1
        Class Program
            Public Property SomeContext As HttpContext
                Get
                    Return Nothing
                End Get
                Set(value As HttpContext)
                End Set
            End Property

            Public  Sub Test(currentContext As HttpContext)
                Dim c = {|#0:HttpContext.Current|}
            End Sub
        End Class
    End Namespace";
            var fixedFile = @"
    Imports System.Web

    Namespace ConsoleApplication1
        Class Program
            Public Property SomeContext As HttpContext
                Get
                    Return Nothing
                End Get
                Set(value As HttpContext)
                End Set
            End Property

            Public  Sub Test(currentContext As HttpContext)
                Dim c = currentContext
        End Sub
        End Class
    End Namespace";

            var expected = VerifyVB.Diagnostic(DiagnosticId).WithLocation(0).WithArguments(HttpContextName, HttpContextCurrentName);
            await CreateTest()
                .WithSource(testFile)
                .WithExpectedDiagnostics(expected)
                .WithFixed(fixedFile)
                .RunAsync();
        }

        [Fact]
        public async Task ReuseParameterName()
        {
            var testFile = @"
    Imports System.Web

    Namespace ConsoleApplication1
        Class Program
            Public Sub Test()
                Test2({|#0:HttpContext.Current|})
            End Sub
            Public Sub Test2(currentContext As HttpContext)
                Test()
            End Sub
        End Class
    End Namespace";
            var fixedFile = @"
    Imports System.Web

    Namespace ConsoleApplication1
        Class Program
            Public Sub Test(currentContext As HttpContext)
                Test2(currentContext)
            End Sub
            Public Sub Test2(currentContext As HttpContext)
                Test(currentContext)
            End Sub
        End Class
    End Namespace";

            var expected = VerifyVB.Diagnostic(DiagnosticId).WithLocation(0).WithArguments(HttpContextName, HttpContextCurrentName);
            await CreateTest()
                .WithSource(testFile)
                .WithExpectedDiagnostics(expected)
                .WithFixed(fixedFile)
                .RunAsync();
        }

        [Fact]
        public async Task InArgument()
        {
            var testFile = @"
    Imports System.Web

    Namespace ConsoleApplication1
        Class Program
            Public Sub Test(currentContext As HttpContext)
            End Sub
            Public Function Test2() As HttpContext
                Return {|#0:HttpContext.Current|}
            End Function
        End Class
    End Namespace";
            var fixedFile = @"
    Imports System.Web

    Namespace ConsoleApplication1
        Class Program
            Public Sub Test(currentContext As HttpContext)
            End Sub
            Public Function Test2(currentContext As HttpContext) As HttpContext
                Return currentContext
        End Function
        End Class
    End Namespace";

            var expected = VerifyVB.Diagnostic(DiagnosticId).WithLocation(0).WithArguments(HttpContextName, HttpContextCurrentName);
            await CreateTest()
                .WithSource(testFile)
                .WithExpectedDiagnostics(expected)
                .WithFixed(fixedFile)
                .RunAsync();
        }

        [Fact]
        public async Task ReplaceCallerInSameDocument()
        {
            var testFile = @"
    Imports System.Web

    Namespace ConsoleApplication1
        Class Program
            Public Sub Test()
                Dim c = {|#0:HttpContext.Current|}
            End Sub
            Public Sub Test2()
                Test()
            End Sub
        End Class
    End Namespace";
            var fixedFile = @"
    Imports System.Web

    Namespace ConsoleApplication1
        Class Program
            Public Sub Test(currentContext As HttpContext)
                Dim c = currentContext
        End Sub
            Public Sub Test2()
                Test({|#0:HttpContext.Current|})
            End Sub
        End Class
    End Namespace";

            var expected1 = VerifyVB.Diagnostic(DiagnosticId).WithLocation(0).WithArguments(HttpContextName, HttpContextCurrentName);
            var expected2 = VerifyVB.Diagnostic(DiagnosticId).WithLocation(0).WithArguments(HttpContextName, HttpContextCurrentName);

            await CreateTest()
                .WithSource(testFile)
                .WithExpectedDiagnostics(expected1)
                .WithFixed(fixedFile)
                .WithExpectedDiagnosticsAfter(expected2)
                .RunAsync();
        }

        [Fact]
        public async Task InProperty()
        {
            var testFile = @"
    Imports System.Web

    Namespace ConsoleApplication1
        Class Program
            Public ReadOnly Property Test As HttpContext
                Get
                    Return {|#0:HttpContext.Current|}
            End Get
            End Property
        End Class
    End Namespace";

            var expected1 = VerifyVB.Diagnostic(DiagnosticId).WithLocation(0).WithArguments(HttpContextName, HttpContextCurrentName);

            await CreateTest()
                .WithSource(testFile)
                .WithExpectedDiagnostics(expected1)
                .WithFixed(testFile)
                .WithExpectedDiagnosticsAfter(expected1)
                .RunAsync();
        }

        [Fact]
        public async Task MultipleFiles()
        {
            var testFile1 = @"
    Imports System.Web

    Namespace ConsoleApplication1
        Class Program
            Public Shared Function Instance() As HttpContext
                Return {|#0:HttpContext.Current|}
            End Function
        End Class
    End Namespace";
            var testFile = @"
    Imports System.Web
    Namespace ConsoleApplication1
        Class Program2
            Public Function Test2() As HttpContext
                Return Program.Instance()
            End Function
        End Class
    End Namespace";

            var fixedFile1 = @"
    Imports System.Web

    Namespace ConsoleApplication1
        Class Program
            Public Shared Function Instance(currentContext As HttpContext) As HttpContext
                Return currentContext
        End Function
        End Class
    End Namespace";
            var fixedFile2 = @"
    Imports System.Web
    Namespace ConsoleApplication1
        Class Program2
            Public Function Test2() As HttpContext
                Return Program.Instance({|#0:HttpContext.Current|})
            End Function
        End Class
    End Namespace";
            var expected = VerifyVB.Diagnostic(DiagnosticId).WithLocation(0).WithArguments(HttpContextName, HttpContextCurrentName);

            await CreateTest()
                .WithSource(testFile1)
                .WithSource(testFile)
                .WithExpectedDiagnostics(expected)
                .WithFixed(fixedFile1)
                .WithFixed(fixedFile2)
                .WithExpectedDiagnosticsAfter(expected)
                .RunAsync();
        }

        [Fact]
        public async Task MultipleFilesNoSystemWebImport()
        {
            var testFile1 = @"
    Imports System.Web

    Namespace ConsoleApplication1
        Class Program
            Public Shared Function Instance() As Object
                Return {|#0:HttpContext.Current|}
            End Function
        End Class
    End Namespace";
            var testFile2 = @"
    Namespace ConsoleApplication1
        Class Program2
            Public Function Test2() As Object
                Return Program.Instance()
            End Function
        End Class
    End Namespace";

            var fixedFile1 = @"
    Imports System.Web

    Namespace ConsoleApplication1
        Class Program
            Public Shared Function Instance(currentContext As HttpContext) As Object
                Return currentContext
        End Function
        End Class
    End Namespace";
            var fixedFile2 = @"
    Namespace ConsoleApplication1
        Class Program2
            Public Function Test2() As Object
                Return Program.Instance({|#0:System.Web.HttpContext.Current|})
            End Function
        End Class
    End Namespace";
            var expected = VerifyVB.Diagnostic(DiagnosticId).WithLocation(0).WithArguments(HttpContextName, HttpContextCurrentName);
            await CreateTest()
                .WithSource(testFile1)
                .WithSource(testFile2)
                .WithExpectedDiagnostics(expected)
                .WithFixed(fixedFile1)
                .WithFixed(fixedFile2)
                .WithExpectedDiagnosticsAfter(expected)

                // We use a generator that ends up not creating the same syntax
                .With(CodeActionValidationMode.None)
                .RunAsync();
        }

        private static VerifyVB.Test CreateTest() => new VerifyVB.Test()
            .WithSystemWeb()
            .With(CodeFixTestBehaviors.FixOne);
    }
}
