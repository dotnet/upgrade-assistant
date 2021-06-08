// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using CS = Microsoft.CodeAnalysis.CSharp;
using VB = Microsoft.CodeAnalysis.VisualBasic;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Common
{
    public readonly struct GeneralInvocationExpression :
        IGeneralizeRoslynSyntax<CS.Syntax.InvocationExpressionSyntax, VB.Syntax.InvocationExpressionSyntax>,
        IEquatable<GeneralInvocationExpression>
    {
        private readonly SyntaxNode _invocationExpression;

        public GeneralInvocationExpression(SyntaxNode syntaxNode)
        {
            if (!syntaxNode.IsInvocationExpression())
            {
                throw new ArgumentException("Invalid type of syntaxNode", nameof(syntaxNode));
            }

            // the only valid way to construct one is to try parse
            // this enables validation to assert we're providing a wrapper around a valid SyntaxNode
            _invocationExpression = syntaxNode ?? throw new ArgumentNullException(nameof(syntaxNode));
        }

        public IEnumerable<SyntaxNode> GetArguments()
        {
            return _invocationExpression.Language switch
            {
                LanguageNames.CSharp => GetCSharpNode().ArgumentList.Arguments.Select(x => x.Expression),
                LanguageNames.VisualBasic => GetVisualBasicNode().ArgumentList.Arguments.Select(x => x.GetExpression()),
                _ => throw new NotSupportedException(nameof(_invocationExpression.Language))
            };
        }

        public Location GetLocation()
        {
            return _invocationExpression.GetLocation();
        }

        public VB.Syntax.InvocationExpressionSyntax GetVisualBasicNode()
        {
            return (VB.Syntax.InvocationExpressionSyntax)_invocationExpression;
        }

        public CS.Syntax.InvocationExpressionSyntax GetCSharpNode()
        {
            return (CS.Syntax.InvocationExpressionSyntax)_invocationExpression;
        }

        public bool Equals(GeneralInvocationExpression other)
        {
            return _invocationExpression.Equals(other._invocationExpression);
        }

        public override bool Equals(object obj)
        {
            if (obj is null || obj is not GeneralInvocationExpression)
            {
                return false;
            }

            return _invocationExpression.Equals(((GeneralInvocationExpression)obj)._invocationExpression);
        }

        public override int GetHashCode()
        {
            return _invocationExpression.GetHashCode();
        }

        public static bool operator ==(GeneralInvocationExpression left, GeneralInvocationExpression right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GeneralInvocationExpression left, GeneralInvocationExpression right)
        {
            return !(left == right);
        }
    }
}
