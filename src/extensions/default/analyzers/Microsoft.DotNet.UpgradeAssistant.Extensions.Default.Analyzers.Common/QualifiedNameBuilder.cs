// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default
{
    /// <summary>
    /// Use this utility to build a QualifiedName from a string.
    /// </summary>
    public static class QualifiedNameBuilder
    {
        /// <summary>
        /// Use this utility to build a QualifiedName from a string.
        /// </summary>
        /// <param name="generator">Call SyntaxGenerator.GetGenerator(document) to get a generator that builds this syntax in a language agnostic way.</param>
        /// <param name="qualifiedName">The string representing qualified syntax (e.g. Microsoft.AspNetCore.Mvc.Controller).</param>
        /// <returns>A QualifiedSyntaxNode. An IdentifierNameSyntax is returned when there is no concept of "left" and "right" nodes.</returns>
        public static SyntaxNode BuildQualifiedNameSyntax(SyntaxGenerator generator, string qualifiedName)
        {
            if (generator is null)
            {
                throw new ArgumentNullException(nameof(generator));
            }

            if (qualifiedName is null || string.IsNullOrWhiteSpace(qualifiedName))
            {
                throw new ArgumentNullException(nameof(qualifiedName));
            }

            // This is not a hot code path currently. If it becomes more heavily used, it may be worthwile to
            // cache the component arrays to reduce array allocation.
            return BuildRecursiveNameSyntax(generator, qualifiedName.Split('.'), generator.QualifiedName);
        }

        /// <summary>
        /// Use this utility to build a MemberAccessExpression from a string.
        /// </summary>
        /// <param name="generator">Call SyntaxGenerator.GetGenerator(document) to get a generator that builds this syntax in a language agnostic way.</param>
        /// <param name="qualifiedName">The string representing a member access expression (e.g. Microsoft.AspNetCore.Mvc.Controller).</param>
        /// <returns>A MemberAccessExpressionSyntaxNode. An IdentifierNameSyntax is returned when there is no concept of "left" and "right" nodes.</returns>
        public static SyntaxNode BuildMemberAccessExpression(SyntaxGenerator generator, string qualifiedName)
        {
            if (generator is null)
            {
                throw new ArgumentNullException(nameof(generator));
            }

            if (qualifiedName is null || string.IsNullOrWhiteSpace(qualifiedName))
            {
                throw new ArgumentNullException(nameof(qualifiedName));
            }

            return BuildRecursiveNameSyntax(generator, qualifiedName.Split('.'), generator.MemberAccessExpression);
        }

        private static SyntaxNode BuildRecursiveNameSyntax(SyntaxGenerator generator, ReadOnlySpan<string> nameSegments, Func<SyntaxNode, SyntaxNode, SyntaxNode> combineFunc)
        {
            if (nameSegments.Length == 1)
            {
                return generator.IdentifierName(nameSegments[0]);
            }

            var rightNode = generator.IdentifierName(nameSegments[nameSegments.Length - 1]);
            var leftNode = BuildRecursiveNameSyntax(generator, nameSegments.Slice(0, nameSegments.Length - 1), combineFunc);
            return combineFunc(leftNode, rightNode);
        }
    }
}
