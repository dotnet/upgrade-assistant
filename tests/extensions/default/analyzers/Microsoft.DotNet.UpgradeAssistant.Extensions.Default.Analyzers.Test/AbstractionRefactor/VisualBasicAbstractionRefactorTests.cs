// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Xunit;

using VerifyVB = Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Test.VisualBasicCodeFixVerifier<
     Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.AdapterRefactorAnalyzer,
     Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CodeFixes.AdapterRefactorCodeFixer>;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Test
{
    public class VisualBasicAbstractionRefactorTests
    {
        [Fact]
        public async Task EmptyCode()
        {
            var testFile = string.Empty;

            await VerifyVB.Create().WithSource(testFile).RunAsync();
        }

        [Fact]
        public async Task JustDefinitionsNothingElse()
        {
            var refactor = new AdapterDescriptorFactory("RefactorTest", "SomeClass", "ISome");
            var testFile = @"
Namespace RefactorTest
    Public Class SomeClass
    End Class

    Public Interface ISome
    End Interface
End Namespace";

            await CreateTest(refactor, withFix: false)
                .WithSource(testFile)
                .RunAsync();
        }

        [Fact]
        public async Task SingleChange()
        {
            var refactor = new AdapterDescriptorFactory("RefactorTest", "SomeClass", "ISome");
            var testFile = @"
Namespace RefactorTest
    Class Test
        Public Sub Helper(c As {|#0:SomeClass|})
        End Sub
    End Class

    Class SomeClass
    End Class

    Interface ISome
    End Interface
End Namespace";

            const string withFix = @"
Namespace RefactorTest
    Class Test
        Public Sub Helper(c As ISome)
        End Sub
    End Class

    Class SomeClass
    End Class

    Interface ISome
    End Interface
End Namespace";

            var diagnostic = VerifyVB.Diagnostic(AdapterRefactorAnalyzer.RefactorDiagnosticId).WithLocation(0).WithArguments(refactor.Original, refactor.Destination);
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
Namespace RefactorTest
    Class Test
        Public Function Helper() As {|#0:SomeClass|}
            Return Nothing
        End Function
    End Class

    Class SomeClass
    End Class

    Interface ISome
    End Interface
End Namespace";

            const string withFix = @"
Namespace RefactorTest
    Class Test
        Public Function Helper() As ISome
            Return Nothing
        End Function
    End Class

    Class SomeClass
    End Class

    Interface ISome
    End Interface
End Namespace";

            var diagnostic = VerifyVB.Diagnostic(AdapterRefactorAnalyzer.RefactorDiagnosticId).WithLocation(0).WithArguments(refactor.Original, refactor.Destination);
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
Namespace RefactorTest
    Class Test
        Private myField As {|#0:SomeClass|}
    End Class

    Class SomeClass
    End Class

    Interface ISome
    End Interface
End Namespace";

            const string withFix = @"
Namespace RefactorTest
    Class Test
        Private myField As {|#0:ISome|}
    End Class

    Class SomeClass
    End Class

    Interface ISome
    End Interface
End Namespace";

            var diagnostic = VerifyVB.Diagnostic(AdapterRefactorAnalyzer.RefactorDiagnosticId).WithLocation(0).WithArguments(refactor.Original, refactor.Destination);
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
Namespace RefactorTest
    Class Test
        Property Instance As {|#0:SomeClass|}
    End Class

    Class SomeClass
    End Class

    Interface ISome
    End Interface
End Namespace";

            const string withFix = @"
Namespace RefactorTest
    Class Test
        Property Instance As {|#0:ISome|}
    End Class

    Class SomeClass
    End Class

    Interface ISome
    End Interface
End Namespace";

            var diagnostic = VerifyVB.Diagnostic(AdapterRefactorAnalyzer.RefactorDiagnosticId).WithLocation(0).WithArguments(refactor.Original, refactor.Destination);
            await CreateTest(refactor)
                .WithSource(testFile)
                .WithExpectedDiagnostics(diagnostic)
                .WithFixed(withFix)
                .RunAsync();
        }

        public static ICodeFixTest CreateTest(AdapterDescriptorFactory? attributeDescriptor = null, bool withFix = true)
        {
            const string Attribute = @"
Imports System

Namespace Microsoft.CodeAnalysis.Refactoring
    Public Class AdapterDescriptorAttribute
        Inherits Attribute

        Public Sub New(interfaceType As Type, original As Type)
        End Sub
    End Class
End Namespace";

            var test = VerifyVB.Create()
                .WithSource(Attribute);

            if (withFix)
            {
                test.WithFixed(Attribute);
            }

            if (attributeDescriptor is not null)
            {
                var descriptor = attributeDescriptor.CreateAttributeString(LanguageNames.VisualBasic);

                test.WithSource(descriptor);

                if (withFix)
                {
                    test.WithFixed(descriptor);
                }
            }

            return test;
        }
    }
}
