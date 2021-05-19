using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.VisualBasic, Name = nameof(VisualBasicHttpContextRefactorCodeFixProvider))]
    [Shared]
    public class VisualBasicHttpContextRefactorCodeFixProvider : HttpContextRefactorCodeFixProvider<InvocationExpressionSyntax, ArgumentSyntax>
    {
        // Methods and properties in VB resolve to an IBlockOperation. We only want methods and not properties so we also check for a MethodBlockSyntax
        private protected override bool IsEnclosedMethodOperation(IOperation operation)
            => operation is IBlockOperation && operation.Syntax is MethodBlockSyntax;

        private protected override InvocationExpressionSyntax AddArgumentToInvocation(InvocationExpressionSyntax invocationNode, ArgumentSyntax argument)
            => invocationNode.WithArgumentList(invocationNode.ArgumentList.AddArguments(argument));
    }
}
