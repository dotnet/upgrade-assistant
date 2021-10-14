// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

using VerifyCS = Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Test.CSharpCodeFixVerifier<
     Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.AdapterDescriptorTypeAnalyzer,
     Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CodeFixes.AdapterRefactorCodeFixer>;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Test
{
    public class AdapterDescriptorTypeAnalyzerTests : AdapterTestBase
    {
        private const string TwoParameters = "Adapter descriptor constructor must have two parameters.";
        private const string MustBeAType = "Descriptor parameters must be a type.";
        private const string MustBeAnAttribute = "Adapter descriptor must be an attribute.";

        [Fact]
        public async Task EmptyCode()
        {
            var testFile = string.Empty;

            await VerifyCS.Create().WithSource(testFile).RunAsync();
        }

        [Fact]
        public async Task CorrectlyFormed()
        {
            var testFile = @"
using System;

namespace Microsoft.CodeAnalysis.Refactoring
{
    public class AdapterDescriptorAttribute : Attribute
    {
        public AdapterDescriptorAttribute(Type destination, Type original)
        {
        }
    }
}";

            await VerifyCS.Create()
                .WithSource(testFile)
                .RunAsync();
        }

        [Fact]
        public async Task NotAnAttribute()
        {
            var testFile = @"
using System;

namespace Microsoft.CodeAnalysis.Refactoring
{
    public class {|#0:AdapterDescriptorAttribute|}
    {
        public AdapterDescriptorAttribute(Type destination, Type original)
        {
        }
    }
}";

            var diagnostic = VerifyCS.Diagnostic().WithLocation(0).WithArguments(MustBeAnAttribute);

            await VerifyCS.Create()
                .WithExpectedDiagnostics(diagnostic)
                .WithSource(testFile)
                .RunAsync();
        }

        [Fact]
        public async Task SingleParameter()
        {
            var testFile = @"
using System;

namespace Microsoft.CodeAnalysis.Refactoring
{
    public class AdapterDescriptorAttribute : Attribute
    {
        public {|#0:AdapterDescriptorAttribute|}(Type type)
        {
        }
    }
}";

            var diagnostic = VerifyCS.Diagnostic().WithLocation(0).WithArguments(TwoParameters);

            await VerifyCS.Create()
                .WithExpectedDiagnostics(diagnostic)
                .WithSource(testFile)
                .RunAsync();
        }

        [Fact]
        public async Task SingleParameterNotAType()
        {
            var testFile = @"
using System;

namespace Microsoft.CodeAnalysis.Refactoring
{
    public class AdapterDescriptorAttribute : Attribute
    {
        public {|#0:AdapterDescriptorAttribute|}(string {|#1:type|})
        {
        }
    }
}";

            var diagnostic1 = VerifyCS.Diagnostic().WithLocation(0).WithArguments(TwoParameters);
            var diagnostic2 = VerifyCS.Diagnostic().WithLocation(1).WithArguments(MustBeAType);

            await VerifyCS.Create()
                .WithExpectedDiagnostics(diagnostic1, diagnostic2)
                .WithSource(testFile)
                .RunAsync();
        }

        [Fact]
        public async Task DefaultConstructor()
        {
            var testFile = @"
using System;

namespace Microsoft.CodeAnalysis.Refactoring
{
    public class {|#0:AdapterDescriptorAttribute|} : Attribute
    {
    }
}";

            var diagnostic = VerifyCS.Diagnostic().WithLocation(0).WithArguments(TwoParameters);

            await VerifyCS.Create()
                .WithExpectedDiagnostics(diagnostic)
                .WithSource(testFile)
                .RunAsync();
        }
    }
}
