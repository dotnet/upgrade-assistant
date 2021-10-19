// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

using VerifyCS = Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Test.CSharpCodeFixVerifier<
     Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.MissingAdapterDescriptor,
     Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CodeFixes.AddAdapterDescriptorCodeFixer>;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Test
{
    public class CSharpAddAdapterDescriptorTests : AdapterTestBase
    {
        private const string AdditionalFileContext = "System.Web.HttpContext";
        private const string AddedHttpContextDescriptor = @"using System.Web;

[assembly: Microsoft.CodeAnalysis.Refactoring.AdapterDescriptor(typeof(HttpContext))]
";

        private const string AddedDescriptorAttributeFile = @"namespace Microsoft.CodeAnalysis.Refactoring
{
    [System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple = true)]
    internal sealed class AdapterDescriptorAttribute : System.Attribute
    {
        public AdapterDescriptorAttribute(System.Type original, System.Type interfaceType = null)
        {
        }
    }
}";

        private const string AdapterDescriptorAttributeFileName = "AdapterDescriptorAttribute.cs";
        private const string DescriptorsFileName = "Descriptors.cs";
        private const string WebRefactoringTxtFileName = "web.refactoring.txt";

        [Fact]
        public async Task EmptyCode()
        {
            var testFile = string.Empty;

            await VerifyCS.Create().WithSource(testFile).WithFixed(testFile).RunAsync();
        }

        [Fact]
        public async Task SingleChangeField()
        {
            var testFile = @"
using System.Web;

namespace RefactorTest
{
    public class SomeClass
    {
        private readonly {|#0:HttpContext|} _context;
    }
}";

            var fixedFile = @"
using System.Web;

namespace RefactorTest
{
    public class SomeClass
    {
        private readonly HttpContext _context;
    }
}";

            var addAttributeDescriptorClassDiagnostic = VerifyCS.Diagnostic().WithLocation(0).WithArguments("HttpContext");
            await VerifyCS.Create()
                .WithSource(testFile)
                .WithAdditionalFile(WebRefactoringTxtFileName, AdditionalFileContext)
                .WithExpectedDiagnostics(addAttributeDescriptorClassDiagnostic)
                .WithFixed(fixedFile)
                .WithFixed(AddedDescriptorAttributeFile, AdapterDescriptorAttributeFileName)
                .WithFixed(AddedHttpContextDescriptor, DescriptorsFileName)
                .WithSystemWeb()
                .RunAsync();
        }

        [Fact]
        public async Task SingleChangeFieldAttributeExistings()
        {
            var testFile = @"
using System;
using System.Web;

namespace RefactorTest
{
    public class SomeClass
    {
        private readonly {|#0:HttpContext|} _context;
    }
}

namespace Microsoft.CodeAnalysis.Refactoring
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    internal sealed class AdapterDescriptorAttribute : Attribute
    {
        public AdapterDescriptorAttribute(Type original, Type interfaceType = null)
        {
        }
    }
}";

            var addAttributeDescriptorClassDiagnostic = VerifyCS.Diagnostic().WithLocation(0).WithArguments("HttpContext");
            await VerifyCS.Create()
                .WithSource(testFile)
                .WithAdditionalFile(WebRefactoringTxtFileName, AdditionalFileContext)
                .WithExpectedDiagnostics(addAttributeDescriptorClassDiagnostic)
                .WithFixed(testFile)
                .WithFixed(AddedHttpContextDescriptor, DescriptorsFileName)
                .WithSystemWeb()
                .RunAsync();
        }

        [Fact]
        public async Task SingleChangeMethodParameter()
        {
            var testFile = @"
using System.Web;

namespace RefactorTest
{
    public class SomeClass
    {
        public string Method({|#0:HttpContext|} context)
        {
            return ""foo"";
        }
    }
}";

            var addAttributeDescriptorClassDiagnostic = VerifyCS.Diagnostic().WithLocation(0).WithArguments("HttpContext");
            await VerifyCS.Create()
                .WithSource(testFile)
                .WithExpectedDiagnostics(addAttributeDescriptorClassDiagnostic)
                .WithAdditionalFile(WebRefactoringTxtFileName, AdditionalFileContext)
                .WithFixed(testFile)
                .WithFixed(AddedDescriptorAttributeFile, AdapterDescriptorAttributeFileName)
                .WithFixed(AddedHttpContextDescriptor, DescriptorsFileName)
                .WithSystemWeb()
                .RunAsync();
        }
    }
}
