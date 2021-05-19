using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CSharpHttpContextRefactorCodeFixProvider))]
    [Shared]
    public class CSharpHttpContextRefactorCodeFixProvider : HttpContextRefactorCodeFixProvider<InvocationExpressionSyntax, ArgumentSyntax>
    {
        private protected override bool IsEnclosedMethodOperation(IOperation operation)
            => operation is IMethodBodyOperation;

        private protected override InvocationExpressionSyntax AddArgumentToInvocation(InvocationExpressionSyntax invocationNode, ArgumentSyntax argument)
            => invocationNode.WithArgumentList(invocationNode.ArgumentList.AddArguments(argument));
    }
}
