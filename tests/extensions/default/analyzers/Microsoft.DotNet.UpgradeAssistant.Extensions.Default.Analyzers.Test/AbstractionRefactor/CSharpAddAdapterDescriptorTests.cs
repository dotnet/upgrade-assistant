// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

using VerifyCS = Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Test.CSharpCodeFixVerifier<
     Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.AdapterDescriptorTypeAnalyzer,
     Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CodeFixes.AddAdapterDescriptorCodeFixer>;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Test
{
    public class CSharpAddAdapterDescriptorTests : AdapterTestBase
    {
        private const string HttpContextAttributeDescriptorClass = @"using System;
using Microsoft.CodeAnalysis.Refactoring;
#if NET || NETCOREAPP
using Microsoft.AspNetCore.Http;
#else
using System.Web;
#endif

[assembly: AdapterDescriptor(typeof(HttpContext))]

namespace Microsoft.CodeAnalysis.Refactoring
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    internal sealed class AdapterDescriptorAttribute : Attribute
    {
        public AdapterDescriptorAttribute(Type original, Type interfaceType)
        {
        }
        public AdapterDescriptorAttribute(Type original)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    internal sealed class AdapterFactoryDescriptorAttribute : Attribute
    {
        public AdapterFactoryDescriptorAttribute(Type factoryType, string factoryMethod)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    internal class AdapterStaticDescriptorAttribute : Attribute
    {
        public AdapterStaticDescriptorAttribute(Type originalType, string originalString, Type destinationType, string destinationString)
        {
        }
    }
}
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

            var addAttributeDescriptorClassDiagnostic = VerifyCS.Diagnostic(AdapterDescriptorTypeAnalyzer.AddAdapterDescriptorDiagnosticId).WithLocation(0).WithArguments("HttpContext");
            await VerifyCS.Create()
                .WithSource(testFile)
                .WithExpectedDiagnostics(addAttributeDescriptorClassDiagnostic)
                .WithFixed(testFile)
                .WithFixed(HttpContextAttributeDescriptorClass, "AdapterDescriptor.cs")
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

            var addAttributeDescriptorClassDiagnostic = VerifyCS.Diagnostic(AdapterDescriptorTypeAnalyzer.AddAdapterDescriptorDiagnosticId).WithLocation(0).WithArguments("HttpContext");
            await VerifyCS.Create()
                .WithSource(testFile)
                .WithExpectedDiagnostics(addAttributeDescriptorClassDiagnostic)
                .WithFixed(testFile)
                .WithFixed(HttpContextAttributeDescriptorClass, "AdapterDescriptor.cs")
                .WithSystemWeb()
                .RunAsync();
        }
    }
}
