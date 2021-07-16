// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers
{
    /// <summary>
    /// Describes a syntax (namspace, type, or member) users might reference in their projects.
    /// </summary>
    public class TargetSyntax
    {
        private string? _fullName;

        /// <summary>
        /// Gets the fully qualified name of the namespace or API.
        /// </summary>
        public string FullName
        {
            get => _fullName!;
            private init
            {
                _fullName = value;
                SimpleName = _fullName.LastIndexOf('.') < 0
                    ? _fullName
                    : _fullName.Substring(_fullName.LastIndexOf('.') + 1);
            }
        }

        /// <summary>
        /// Gets the simple name of the namespace or API.
        /// </summary>
        public string SimpleName { get; private init; } = default!;

        /// <summary>
        /// Gets the type of syntax.
        /// </summary>
        public TargetSyntaxType SyntaxType { get; }

        /// <summary>
        /// Gets a value indicating whether the syntax should be matched if the user
        /// uses syntax that ambiguously matches it. For example, if the simple name and
        /// type match but no symbolic information is available and the used syntax doesn't have a
        /// fully qualified name, the syntax will only be matched if this property is true.
        /// </summary>
        public bool AlertOnAmbiguousMatch { get; }

        /// <summary>
        /// Gets a NameMatcher that can be used to determine whether syntax nodes or symbols
        /// matche this target syntax.
        /// </summary>
        public NameMatcher NameMatcher { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TargetSyntax"/> class.
        /// </summary>
        /// <param name="fullName">The full name of the namespace or API.</param>
        /// <param name="syntaxType">The type of the syntax.</param>
        /// <param name="alertOnAmbiguousMatch">True if the syntax should be matched if only the simple name is matched and the full name is unknown.</param>
        public TargetSyntax(string fullName, TargetSyntaxType syntaxType, bool alertOnAmbiguousMatch)
        {
            FullName = fullName ?? throw new ArgumentNullException(nameof(fullName));
            SyntaxType = syntaxType;
            AlertOnAmbiguousMatch = alertOnAmbiguousMatch;
            NameMatcher = syntaxType is TargetSyntaxType.Member
                ? NameMatcher.MatchMemberAccess(fullName)
                : NameMatcher.MatchType(fullName);
        }
    }
}
