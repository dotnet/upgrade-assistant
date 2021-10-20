// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

using VerifyCS = Microsoft.DotNet.UpgradeAssistant.DeprecatedApisAnalyzer.Test.CSharpCodeFixVerifier<
     Microsoft.DotNet.UpgradeAssistant.DeprecatedApisAnalyzer.AdapterDefinitionAnalyzer,
     Microsoft.DotNet.UpgradeAssistant.DeprecatedApisAnalyzer.CodeFixes.AdapterDefinitionCodeFixer>;

namespace Microsoft.DotNet.UpgradeAssistant.DeprecatedApisAnalyzer.Test.AbstractionRefactor
{
    public class CSharpAdapterDefinitionTests : AdapterTestBase
    {
        [Fact]
        public async Task CanGenerateInterfaceStub()
        {
            var testFile = @$"
[assembly: {{|#0:{WellKnownTypeNames.AdapterDescriptorFullyQualified}(typeof(TestProject.SomeClass))|}}]
" + @"
namespace TestProject
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

            const string withFix = @$"
[assembly: {{|#0:{WellKnownTypeNames.AdapterDescriptorFullyQualified}(typeof(TestProject.SomeClass), typeof(TestProject.ISomeClass))|}}]
" + @"
namespace TestProject
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

            const string interfaceDefinition = @"namespace TestProject
{
    public interface ISomeClass
    {
    }
}";

            var diagnostic = VerifyCS.Diagnostic(AdapterDefinitionAnalyzer.DefinitionDiagnosticId).WithLocation(0).WithArguments("TestProject.SomeClass");
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
            var testFile = @$"
[assembly: {{|#0:{WellKnownTypeNames.AdapterDescriptorFullyQualified}(typeof(TestProject.SomeClass))|}}]
" + @"
namespace TestProject
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

            const string withFix = @$"
[assembly: {{|#0:{WellKnownTypeNames.AdapterDescriptorFullyQualified}(typeof(TestProject.SomeClass), typeof(TestProject.ISomeClass))|}}]
" + @"
namespace TestProject
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

            var diagnostic = VerifyCS.Diagnostic(AdapterDefinitionAnalyzer.DefinitionDiagnosticId).WithLocation(0).WithArguments("TestProject.SomeClass");
            await CreateTest(VerifyCS.Create(), null)
                .WithSource(testFile)
                .WithExpectedDiagnostics(diagnostic)
                .WithFixed(withFix)
                .RunAsync();
        }

        [Fact]
        public async Task CanGenerateInterfaceStubWithCorrectNamespace()
        {
            var testFile = @$"
using ClassLib;

[assembly: {{|#0:{WellKnownTypeNames.AdapterDescriptorFullyQualified}(typeof(ClassLib.SomeClass))|}}]
" + @"
namespace TestProject
{
    public class Test
    {
        public void Run(SomeClass c)
        {
            var isValid = c.IsValid();
        }
    }
}";
            var concreteFile = @"
namespace ClassLib
{
    public class SomeClass
    {
        public bool IsValid() => true;
    }
}";

            const string withFix = @$"
using ClassLib;

[assembly: {{|#0:{WellKnownTypeNames.AdapterDescriptorFullyQualified}(typeof(ClassLib.SomeClass), typeof(TestProject.ISomeClass))|}}]
" + @"
namespace TestProject
{
    public class Test
    {
        public void Run(SomeClass c)
        {
            var isValid = c.IsValid();
        }
    }
}";

            const string interfaceDefinition = @"namespace TestProject
{
    public interface ISomeClass
    {
    }
}";

            var diagnostic = VerifyCS.Diagnostic(AdapterDefinitionAnalyzer.DefinitionDiagnosticId).WithLocation(0).WithArguments("ClassLib.SomeClass");
            await CreateTest(VerifyCS.Create(), null)
                .WithSource(testFile)
                .WithSource(concreteFile)
                .WithExpectedDiagnostics(diagnostic)
                .WithFixed(withFix)
                .WithFixed(concreteFile)
                .WithFixed(interfaceDefinition, "ISomeClass.cs")
                .RunAsync();
        }
    }
}
