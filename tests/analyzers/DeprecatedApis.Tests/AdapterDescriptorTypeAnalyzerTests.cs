// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

using VerifyCS = Microsoft.DotNet.UpgradeAssistant.DeprecatedApisAnalyzer.Test.CSharpCodeFixVerifier<
     Microsoft.DotNet.UpgradeAssistant.DeprecatedApisAnalyzer.AdapterDescriptorTypeAnalyzer,
     Microsoft.DotNet.UpgradeAssistant.DeprecatedApisAnalyzer.CodeFixes.AdapterRefactorCodeFixer>;

namespace Microsoft.DotNet.UpgradeAssistant.DeprecatedApisAnalyzer.Test
{
    public class AdapterDescriptorTypeAnalyzerTests : AdapterTestBase
    {
        private const string SystemTypeName = "System.Type";
        private const string StringName = "System.String";
        private const string Period = ".";

        [Fact]
        public async Task EmptyCode()
        {
            var testFile = string.Empty;

            await VerifyCS.Create().WithSource(testFile).RunAsync();
        }

        [InlineData(WellKnownTypeNames.AdapterDescriptor, new[] { SystemTypeName, SystemTypeName })]
        [InlineData(WellKnownTypeNames.AdapterStaticDescriptor, new[] { SystemTypeName, StringName, SystemTypeName, StringName })]
        [InlineData(WellKnownTypeNames.FactoryDescriptor, new[] { SystemTypeName, StringName })]
        [Theory]
        public async Task CorrectlyFormed(string attributeName, string[] types)
        {
            var testFile = @$"
using System;

namespace {WellKnownTypeNames.AttributeNamespace}
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

        [InlineData(WellKnownTypeNames.AdapterDescriptor, new[] { SystemTypeName, SystemTypeName })]
        [InlineData(WellKnownTypeNames.AdapterStaticDescriptor, new[] { SystemTypeName, StringName, SystemTypeName, StringName })]
        [InlineData(WellKnownTypeNames.FactoryDescriptor, new[] { SystemTypeName, StringName })]
        [Theory]
        public async Task NotAnAttribute(string attributeName, string[] types)
        {
            var testFile = @$"
using System;

namespace {WellKnownTypeNames.AttributeNamespace}
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
                .WithArguments(WellKnownTypeNames.AttributeNamespace + Period + attributeName);

            await VerifyCS.Create()
                .WithExpectedDiagnostics(diagnostic)
                .WithSource(testFile)
                .RunAsync();
        }

        [InlineData(WellKnownTypeNames.AdapterDescriptor)]
        [InlineData(WellKnownTypeNames.AdapterStaticDescriptor)]
        [InlineData(WellKnownTypeNames.FactoryDescriptor)]
        [Theory]
        public async Task SingleParameter(string attributeName)
        {
            var testFile = @$"
using System;

namespace {WellKnownTypeNames.AttributeNamespace}
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
                .WithArguments(WellKnownTypeNames.AttributeNamespace + Period + attributeName, 2);

            await VerifyCS.Create()
                .WithExpectedDiagnostics(diagnostic)
                .WithSource(testFile)
                .RunAsync();
        }

        [InlineData(WellKnownTypeNames.AdapterDescriptor, SystemTypeName)]
        [InlineData(WellKnownTypeNames.AdapterStaticDescriptor, SystemTypeName)]
        [InlineData(WellKnownTypeNames.FactoryDescriptor, SystemTypeName)]
        [Theory]
        public async Task SingleParameterNotAType(string attributeName, string typeName)
        {
            var testFile = @$"
using System;

namespace {WellKnownTypeNames.AttributeNamespace}
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
                .WithArguments(WellKnownTypeNames.AttributeNamespace + Period + attributeName, 2);
            var diagnostic2 = VerifyCS.Diagnostic(AdapterDescriptorTypeAnalyzer.ParameterDiagnosticId)
                .WithLocation(1)
                .WithArguments(WellKnownTypeNames.AttributeNamespace + Period + attributeName, typeName);

            await VerifyCS.Create()
                .WithExpectedDiagnostics(diagnostic1, diagnostic2)
                .WithSource(testFile)
                .RunAsync();
        }

        [InlineData(WellKnownTypeNames.AdapterDescriptor)]
        [InlineData(WellKnownTypeNames.AdapterStaticDescriptor)]
        [InlineData(WellKnownTypeNames.FactoryDescriptor)]
        [Theory]
        public async Task DefaultConstructor(string attributeName)
        {
            var testFile = @$"
using System;

namespace {WellKnownTypeNames.AttributeNamespace}
{{
    public class {{|#0:{attributeName}|}} : Attribute
    {{
    }}
}}";

            var diagnostic = VerifyCS.Diagnostic(AdapterDescriptorTypeAnalyzer.ParameterCountDiagnosticId)
                .WithLocation(0)
                .WithArguments(WellKnownTypeNames.AttributeNamespace + Period + attributeName, 2);

            await VerifyCS.Create()
                .WithExpectedDiagnostics(diagnostic)
                .WithSource(testFile)
                .RunAsync();
        }

        private static string JoinTypes(string[] types)
            => string.Join(", ", types.Select(t => $"{t} _{Guid.NewGuid():N}"));
    }
}
