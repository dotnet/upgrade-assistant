// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CSharp.CodeFixes
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
        public static SyntaxNode BuildQualifiedSyntax(SyntaxGenerator generator, string qualifiedName)
        {
            if (generator is null)
            {
                throw new ArgumentNullException(nameof(generator));
            }

            if (qualifiedName is null || string.IsNullOrWhiteSpace(qualifiedName))
            {
                throw new ArgumentNullException(nameof(qualifiedName));
            }

            return BuildQualifiedSyntax(generator, qualifiedName.Split('.'));
        }

        private static SyntaxNode BuildQualifiedSyntax(SyntaxGenerator generator, ReadOnlySpan<string> nameSegments)
        {
            if (generator is null)
            {
                throw new ArgumentNullException(nameof(generator));
            }

            if (nameSegments.Length == 0)
            {
                throw new ArgumentNullException(nameof(nameSegments));
            }
            else if (nameSegments.Length == 1)
            {
                return generator.IdentifierName(nameSegments[0]);
            }

            var rightNode = generator.IdentifierName(nameSegments[nameSegments.Length - 1]);
            var leftNode = BuildQualifiedSyntax(generator, nameSegments.Slice(0, nameSegments.Length - 1));
            return generator.QualifiedName(leftNode, rightNode);
        }
    }
}
