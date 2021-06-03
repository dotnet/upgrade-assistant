﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using CS = Microsoft.CodeAnalysis.CSharp;
using VB = Microsoft.CodeAnalysis.VisualBasic;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Common
{
    public class GeneralMemberAccessExpression
    {
        private readonly SyntaxNode _memberAccessExpression;

        public GeneralMemberAccessExpression(SyntaxNode syntaxNode)
        {
            if (!syntaxNode.IsMemberAccessExpression())
            {
                throw new ArgumentException("Invalid type of syntaxNode", nameof(syntaxNode));
            }

            // the only valid way to construct one is to try parse
            // this enables validation to assert we're providing a wrapper around a valid SyntaxNode
            _memberAccessExpression = syntaxNode ?? throw new ArgumentNullException(nameof(syntaxNode));
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
                LanguageNames.CSharp => _memberAccessExpression.Parent is CS.Syntax.InvocationExpressionSyntax,
                LanguageNames.VisualBasic => _memberAccessExpression.Parent is VB.Syntax.InvocationExpressionSyntax,
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
        /// <returns>For new BinaryFormatter().UnsafeDeserialize(x,y) this should return the "new BinaryFormatter()" portion.</returns>
        public SyntaxNode GetAccessedIdentifier()
        {
            return _memberAccessExpression.Language switch
            {
                LanguageNames.CSharp => GetCSharpNode().Expression.GetQualifiedName(),
                LanguageNames.VisualBasic => GetVisualBasicNode().Expression.GetQualifiedName(),
                _ => throw new NotSupportedException(nameof(_memberAccessExpression.Language))
            };
        }

        private VB.Syntax.MemberAccessExpressionSyntax GetVisualBasicNode()
        {
            return (VB.Syntax.MemberAccessExpressionSyntax)_memberAccessExpression;
        }

        private CS.Syntax.MemberAccessExpressionSyntax GetCSharpNode()
        {
            return (CS.Syntax.MemberAccessExpressionSyntax)_memberAccessExpression;
        }
    }
}