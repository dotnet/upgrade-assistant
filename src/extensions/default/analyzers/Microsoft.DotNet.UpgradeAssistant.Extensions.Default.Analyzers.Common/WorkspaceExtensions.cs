// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;

#pragma warning disable CA1062 // Validate arguments of public methods

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default
{
    public static class WorkspaceExtensions
    {
        public static bool TargetsAspNetCore(this Compilation compilation)
            => compilation.ReferencedAssemblyNames.Any(n => n.Name.StartsWith("Microsoft.AspNetCore", StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Tries to find a document given the <see cref="SyntaxTree"/> provided.
        /// </summary>
        /// <param name="sln">The solution to search in.</param>
        /// <param name="tree">The tree to match.</param>
        /// <param name="document">The resulting document.</param>
        /// <returns>Whether the search was successful or not.</returns>
        public static bool TryGetDocument(this Solution sln, SyntaxTree? tree, [MaybeNullWhen(false)] out Document document)
        {
            if (tree is null || sln is null)
            {
                document = null;
                return false;
            }

            foreach (var project in sln.Projects)
            {
                var doc = project.GetDocument(tree);

                if (doc is not null)
                {
                    document = doc;
                    return true;
                }
            }

            document = null;
            return false;
        }
    }
}
