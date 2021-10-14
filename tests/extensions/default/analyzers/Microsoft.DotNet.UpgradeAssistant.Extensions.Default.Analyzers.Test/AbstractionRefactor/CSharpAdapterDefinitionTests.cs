// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

using VerifyCS = Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Test.CSharpCodeFixVerifier<
     Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.AdapterDefinitionAnalyzer,
     Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CodeFixes.AdapterDefinitionCodeFixer>;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Test.AbstractionRefactor
{
    public class CSharpAdapterDefinitionTests : AdapterTestBase
    {
        [Fact]
        public async Task CanGenerateInterfaceStub()
        {
            var testFile = @"
[assembly: {|#0:Microsoft.CodeAnalysis.Refactoring.AdapterDescriptor(typeof(RefactorTest.SomeClass))|}]

namespace RefactorTest
{
    public class Test
    {
        public void Run(SomeClass c)
        {
            var isValid = c.IsValid();
        }
    }

    public class SomeClass
    {
       public bool IsValid() => true;
    }
}";

            const string withFix = @"
[assembly: Microsoft.CodeAnalysis.Refactoring.AdapterDescriptor(typeof(RefactorTest.ISomeClass), typeof(RefactorTest.SomeClass))]

namespace RefactorTest
{
    public class Test
    {
        public void Run(SomeClass c)
        {
            var isValid = c.IsValid();
        }
    }

    public class SomeClass
    {
       public bool IsValid() => true;
    }
}";

            const string interfaceDefinition = @"namespace RefactorTest
{
    public interface ISomeClass
    {
    }
}";

            var diagnostic = VerifyCS.Diagnostic(AdapterDefinitionAnalyzer.DefinitionDiagnosticId).WithLocation(0).WithArguments("RefactorTest.SomeClass");
            await CreateTest(VerifyCS.Create(), null)
                .WithSource(testFile)
                .WithExpectedDiagnostics(diagnostic)
                .WithFixed(withFix)
                .WithFixed(interfaceDefinition, "ISomeClass.cs")
                .RunAsync();
        }

        [Fact]
        public async Task DoesNothingIfInterfaceStubExists()
        {
            var testFile = @"
[assembly: {|#0:Microsoft.CodeAnalysis.Refactoring.AdapterDescriptor(typeof(RefactorTest.SomeClass))|}]

namespace RefactorTest
{
    public class Test
    {
        public void Run(SomeClass c)
        {
            var isValid = c.IsValid();
        }
    }

    public class SomeClass
    {
       public bool IsValid() => true;
    }

    public interface ISomeClass
    {
    }
}";

            const string withFix = @"
[assembly: Microsoft.CodeAnalysis.Refactoring.AdapterDescriptor(typeof(RefactorTest.ISomeClass), typeof(RefactorTest.SomeClass))]

namespace RefactorTest
{
    public class Test
    {
        public void Run(SomeClass c)
        {
            var isValid = c.IsValid();
        }
    }

    public class SomeClass
    {
       public bool IsValid() => true;
    }

    public interface ISomeClass
    {
    }
}";

            var diagnostic = VerifyCS.Diagnostic(AdapterDefinitionAnalyzer.DefinitionDiagnosticId).WithLocation(0).WithArguments("RefactorTest.SomeClass");
            await CreateTest(VerifyCS.Create(), null)
                .WithSource(testFile)
                .WithExpectedDiagnostics(diagnostic)
                .WithFixed(withFix)
                .RunAsync();
        }
    }
}
