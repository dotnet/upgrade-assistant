// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
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
        private const string StringName = "System.String";
        private const string RefactoringNamespace = "Microsoft.CodeAnalysis.Refactoring.";
        private const string AdapterDescriptorClassName = "AdapterDescriptorAttribute";
        private const string AdapterStaticDescriptorClassName = "AdapterStaticDescriptorAttribute";
        private const string FactoryDescriptorClassName = "AdapterFactoryDescriptorAttribute";

        [Fact]
        public async Task EmptyCode()
        {
            var testFile = string.Empty;

            await VerifyCS.Create().WithSource(testFile).RunAsync();
        }

        [InlineData(AdapterDescriptorClassName, new[] { SystemTypeName, SystemTypeName })]
        [InlineData(AdapterStaticDescriptorClassName, new[] { SystemTypeName, StringName, SystemTypeName, StringName })]
        [InlineData(FactoryDescriptorClassName, new[] { SystemTypeName, StringName })]
        [Theory]
        public async Task CorrectlyFormed(string attributeName, string[] types)
        {
            var testFile = @$"
using System;

namespace Microsoft.CodeAnalysis.Refactoring
{{
    public class {attributeName} : Attribute
    {{
        public {attributeName}({JoinTypes(types)})
        {{
        }}
    }}
}}";

            await VerifyCS.Create()
                .WithSource(testFile)
                .RunAsync();
        }

        [InlineData(AdapterDescriptorClassName, new[] { SystemTypeName, SystemTypeName })]
        [InlineData(AdapterStaticDescriptorClassName, new[] { SystemTypeName, StringName, SystemTypeName, StringName })]
        [InlineData(FactoryDescriptorClassName, new[] { SystemTypeName, StringName })]
        [Theory]
        public async Task NotAnAttribute(string attributeName, string[] types)
        {
            var testFile = @$"
using System;

namespace Microsoft.CodeAnalysis.Refactoring
{{
    public class {{|#0:{attributeName}|}}
    {{
        public {attributeName}({JoinTypes(types)})
        {{
        }}
    }}
}}";

            var diagnostic = VerifyCS.Diagnostic(AdapterDescriptorTypeAnalyzer.AttributeDiagnosticId)
                .WithLocation(0)
                .WithArguments(RefactoringNamespace + attributeName);

            await VerifyCS.Create()
                .WithExpectedDiagnostics(diagnostic)
                .WithSource(testFile)
                .RunAsync();
        }

        [InlineData(AdapterDescriptorClassName)]
        [InlineData(AdapterStaticDescriptorClassName)]
        [InlineData(FactoryDescriptorClassName)]
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
                .WithArguments(RefactoringNamespace + attributeName, 2);

            await VerifyCS.Create()
                .WithExpectedDiagnostics(diagnostic)
                .WithSource(testFile)
                .RunAsync();
        }

        [InlineData(AdapterDescriptorClassName, SystemTypeName)]
        [InlineData(AdapterStaticDescriptorClassName, SystemTypeName)]
        [InlineData(FactoryDescriptorClassName, SystemTypeName)]
        [Theory]
        public async Task SingleParameterNotAType(string attributeName, string typeName)
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
                .WithArguments(RefactoringNamespace + attributeName, 2);
            var diagnostic2 = VerifyCS.Diagnostic(AdapterDescriptorTypeAnalyzer.ParameterDiagnosticId)
                .WithLocation(1)
                .WithArguments(RefactoringNamespace + attributeName, typeName);

            await VerifyCS.Create()
                .WithExpectedDiagnostics(diagnostic1, diagnostic2)
                .WithSource(testFile)
                .RunAsync();
        }

        [InlineData(AdapterDescriptorClassName)]
        [InlineData(AdapterStaticDescriptorClassName)]
        [InlineData(FactoryDescriptorClassName)]
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
                .WithArguments(RefactoringNamespace + attributeName, 2);

            await VerifyCS.Create()
                .WithExpectedDiagnostics(diagnostic)
                .WithSource(testFile)
                .RunAsync();
        }

        private static string JoinTypes(string[] types)
            => string.Join(", ", types.Select(t => $"{t} _{Guid.NewGuid():N}"));
    }
}
