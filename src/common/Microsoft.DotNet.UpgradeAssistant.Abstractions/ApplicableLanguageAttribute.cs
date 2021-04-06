// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.UpgradeAssistant
{
    using System;

    /// <summary>
    /// An attribute for marking code fix providers that are only applicable when
    /// the project being upgraded contains certain language.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ApplicableLanguageAttribute : Attribute
    {
        /// <summary>
        /// Gets the language that a project must contain for the attributed feature to apply.
        /// </summary>
        public Language Language { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicableLanguageAttribute"/> class.
        /// </summary>
        /// <param name="language">The language that a project must contain for the attributed
        /// code fix provider to apply to it.</param>
        public ApplicableLanguageAttribute(Language language)
        {
            Language = language;
        }
    }
}
