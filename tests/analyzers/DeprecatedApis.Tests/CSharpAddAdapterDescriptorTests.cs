// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Xunit;

using VerifyCS = Microsoft.DotNet.UpgradeAssistant.DeprecatedApisAnalyzer.Test.CSharpCodeFixVerifier<
     Microsoft.DotNet.UpgradeAssistant.DeprecatedApisAnalyzer.MissingAdapterDescriptor,
     Microsoft.DotNet.UpgradeAssistant.DeprecatedApisAnalyzer.CodeFixes.AddAdapterDescriptorCodeFixer>;

namespace Microsoft.DotNet.UpgradeAssistant.DeprecatedApisAnalyzer.Test
{
    public class CSharpAddAdapterDescriptorTests : AdapterTestBase
    {
        private const string AdapterDescriptorAttributeFileName = "AdapterDescriptorAttribute.cs";
        private const string DescriptorsFileName = "Descriptors.cs";
        private const string WebRefactoringTxtFileName = "web.refactoring.txt";
        private const string AdditionalFileContext = "System.Web.HttpContext";

        private const string AddedDescriptorAttributeFile = @$"namespace {WellKnownTypeNames.AttributeNamespace}
{{
    [System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple = true)]
    internal sealed class {WellKnownTypeNames.AdapterDescriptor} : System.Attribute
    {{
        public {WellKnownTypeNames.AdapterDescriptor}(System.Type original, System.Type interfaceType = null)
        {{
        }}
    }}
}}";

        private static readonly string AttributeWithoutSuffix = WellKnownTypeNames.AdapterDescriptorFullyQualified.AsSpan()[..^"Attribute".Length].ToString();
        private static readonly string AddedHttpContextDescriptor = @$"using System.Web;

[assembly: {AttributeWithoutSuffix}(typeof(HttpContext))]
";

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

" + AddedDescriptorAttributeFile;

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
