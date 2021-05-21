// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using cSharp = Microsoft.CodeAnalysis.CSharp;
using visualBasic = Microsoft.CodeAnalysis.VisualBasic;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Common
{
    public class GeneralMemberAccessExpression
    {
        private readonly SyntaxNode _memberAccessExpression;

        private GeneralMemberAccessExpression(SyntaxNode aMemberAccessExpression)
        {
            _memberAccessExpression = aMemberAccessExpression ?? throw new ArgumentNullException(nameof(aMemberAccessExpression));
        }

        public static bool TryGetGeneralMemberAccessExpress(SyntaxNode node, [MaybeNullWhen(false)] out GeneralMemberAccessExpression expression)
        {
            if (!node.IsMemberAccessExpressionSyntax())
            {
                expression = null;
                return false;
            }

            expression = new GeneralMemberAccessExpression(node);
            return true;
        }

        private visualBasic.Syntax.MemberAccessExpressionSyntax GetVisualBasicNode()
        {
            return (visualBasic.Syntax.MemberAccessExpressionSyntax)_memberAccessExpression;
        }

        private cSharp.Syntax.MemberAccessExpressionSyntax GetCSharpNode()
        {
            return (cSharp.Syntax.MemberAccessExpressionSyntax)_memberAccessExpression;
        }

        public string GetName()
        {
            return _memberAccessExpression.Language switch
            {
                LanguageNames.CSharp => GetCSharpNode().Name.ToString(),
                LanguageNames.VisualBasic => GetVisualBasicNode().Name.ToString(),
                _ => throw new NotSupportedException(nameof(_memberAccessExpression.Language))
            };
        }

        public bool IsChildOfInvocationExpression()
        {
            return _memberAccessExpression.Language switch
            {
                LanguageNames.CSharp => _memberAccessExpression.Parent is cSharp.Syntax.InvocationExpressionSyntax,
                LanguageNames.VisualBasic => _memberAccessExpression.Parent is visualBasic.Syntax.InvocationExpressionSyntax,
                _ => throw new NotSupportedException(nameof(_memberAccessExpression.Language))
            };
        }

        public Location? GetLocation()
        {
            return _memberAccessExpression.GetLocation();
        }

        /// <summary>
        /// Finds the name of the object being accessed.
        /// </summary>
        /// <returns>For new BinaryFormatter().UnsafeDeserialize(x,y) this should return the "new BinaryFormatter()" portion</returns>
        public SyntaxNode GetAccessedIdentifier()
        {
            return _memberAccessExpression.Language switch
            {
                LanguageNames.CSharp => GetCSharpNode().Expression.GetQualifiedName(),
                LanguageNames.VisualBasic => GetVisualBasicNode().Expression.GetQualifiedName(),
                _ => throw new NotSupportedException(nameof(_memberAccessExpression.Language))
            };
        }
    }
}
