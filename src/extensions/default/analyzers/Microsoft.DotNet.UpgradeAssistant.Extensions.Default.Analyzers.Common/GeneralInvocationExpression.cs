// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using cSharp = Microsoft.CodeAnalysis.CSharp;
using visualBasic = Microsoft.CodeAnalysis.VisualBasic;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Common
{
    public class GeneralInvocationExpression
    {
        private readonly SyntaxNode _invocationExpression;

        private GeneralInvocationExpression(SyntaxNode syntaxNode)
        {
            // the only valid way to construct one is to try parse
            // this enables validation to assert we're providing a wrapper around a valid SyntaxNode
            _invocationExpression = syntaxNode ?? throw new ArgumentNullException(nameof(syntaxNode));
        }

        public IEnumerable<string> GetArguments()
        {
            return _invocationExpression.Language switch
            {
                LanguageNames.CSharp => GetCSharpNode().ArgumentList.Arguments.Select(x => x.Expression.Kind().ToString()),
                LanguageNames.VisualBasic => GetVisualBasicNode().ArgumentList.Arguments.Select(x => x.GetExpression().Kind().ToString()),
                _ => throw new NotSupportedException(nameof(_invocationExpression.Language))
            };
        }

        public static bool TryParse(SyntaxNode node, [MaybeNullWhen(false)] out GeneralInvocationExpression expression)
        {
            if (!node.IsInvocationExpression())
            {
                expression = null;
                return false;
            }

            expression = new GeneralInvocationExpression(node);
            return true;
        }

        private visualBasic.Syntax.InvocationExpressionSyntax GetVisualBasicNode()
        {
            return (visualBasic.Syntax.InvocationExpressionSyntax)_invocationExpression;
        }

        private cSharp.Syntax.InvocationExpressionSyntax GetCSharpNode()
        {
            return (cSharp.Syntax.InvocationExpressionSyntax)_invocationExpression;
        }
    }
}
