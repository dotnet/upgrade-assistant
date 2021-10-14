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
        private const string SystemTypeName = "System.Type";
        private const string AdapterDescriptorClassName = "AdapterDescriptorAttribute";
        private const string AdapterDescriptorFullyQualifiedName = "Microsoft.CodeAnalysis.Refactoring." + AdapterDescriptorClassName;

        [Fact]
        public async Task EmptyCode()
        {
            var testFile = string.Empty;

            await VerifyCS.Create().WithSource(testFile).RunAsync();
        }

        [InlineData(AdapterDescriptorClassName, SystemTypeName)]
        [Theory]
        public async Task CorrectlyFormed(string attributeName, string type2)
        {
            var testFile = @$"
using System;

namespace Microsoft.CodeAnalysis.Refactoring
{{
    public class {attributeName} : Attribute
    {{
        public {attributeName}(Type destination, {type2} original)
        {{
        }}
    }}
}}";

            await VerifyCS.Create()
                .WithSource(testFile)
                .RunAsync();
        }

        [InlineData(AdapterDescriptorClassName, SystemTypeName)]
        [Theory]
        public async Task NotAnAttribute(string attributeName, string type2)
        {
            var testFile = @$"
using System;

namespace Microsoft.CodeAnalysis.Refactoring
{{
    public class {{|#0:{attributeName}|}}
    {{
        public {attributeName}(Type destination, {type2} original)
        {{
        }}
    }}
}}";

            var diagnostic = VerifyCS.Diagnostic(AdapterDescriptorTypeAnalyzer.AttributeDiagnosticId)
                .WithLocation(0)
                .WithArguments(AdapterDescriptorFullyQualifiedName);

            await VerifyCS.Create()
                .WithExpectedDiagnostics(diagnostic)
                .WithSource(testFile)
                .RunAsync();
        }

        [InlineData(AdapterDescriptorClassName)]
        [Theory]
        public async Task SingleParameter(string attributeName)
        {
            var testFile = @$"
using System;

namespace Microsoft.CodeAnalysis.Refactoring
{{
    public class {attributeName} : Attribute
    {{
        public {{|#0:{attributeName}|}}(Type destination)
        {{
        }}
    }}
}}";

            var diagnostic = VerifyCS.Diagnostic(AdapterDescriptorTypeAnalyzer.ParameterCountDiagnosticId)
                .WithLocation(0)
                .WithArguments(AdapterDescriptorFullyQualifiedName, 2);

            await VerifyCS.Create()
                .WithExpectedDiagnostics(diagnostic)
                .WithSource(testFile)
                .RunAsync();
        }

        [InlineData(AdapterDescriptorClassName)]
        [Theory]
        public async Task SingleParameterNotAType(string attributeName)
        {
            var testFile = @$"
using System;

namespace Microsoft.CodeAnalysis.Refactoring
{{
    public class {attributeName} : Attribute
    {{
        public {{|#0:{attributeName}|}}(string {{|#1:destination|}})
        {{
        }}
    }}
}}";

            var diagnostic1 = VerifyCS.Diagnostic(AdapterDescriptorTypeAnalyzer.ParameterCountDiagnosticId)
                .WithLocation(0)
                .WithArguments(AdapterDescriptorFullyQualifiedName, 2);
            var diagnostic2 = VerifyCS.Diagnostic(AdapterDescriptorTypeAnalyzer.ParameterDiagnosticId)
                .WithLocation(1)
                .WithArguments(AdapterDescriptorFullyQualifiedName, SystemTypeName);

            await VerifyCS.Create()
                .WithExpectedDiagnostics(diagnostic1, diagnostic2)
                .WithSource(testFile)
                .RunAsync();
        }

        [InlineData(AdapterDescriptorClassName)]
        [Theory]
        public async Task DefaultConstructor(string attributeName)
        {
            var testFile = @$"
using System;

namespace Microsoft.CodeAnalysis.Refactoring
{{
    public class {{|#0:{attributeName}|}} : Attribute
    {{
    }}
}}";

            var diagnostic = VerifyCS.Diagnostic(AdapterDescriptorTypeAnalyzer.ParameterCountDiagnosticId)
                .WithLocation(0)
                .WithArguments(AdapterDescriptorFullyQualifiedName, 2);

            await VerifyCS.Create()
                .WithExpectedDiagnostics(diagnostic)
                .WithSource(testFile)
                .RunAsync();
        }
    }
}
