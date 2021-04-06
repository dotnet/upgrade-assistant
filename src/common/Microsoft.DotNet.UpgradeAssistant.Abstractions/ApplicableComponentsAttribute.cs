// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.UpgradeAssistant
{
    using System;

    /// <summary>
    /// An attribute for marking code fix providers that are only applicable when
    /// the project being upgraded contains certain components.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ApplicableComponentsAttribute : Attribute
    {
        /// <summary>
        /// Gets the components that a project must contain for the attributed code fix provider to apply.
        /// In order for the attributed code fix provider to apply, the project must contain all of
        /// the indicated components (but it can also contain more).
        /// </summary>
        public ProjectComponents Components { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicableComponentsAttribute"/> class.
        /// </summary>
        /// <param name="components">The components that a project must contain for the attributed
        /// code fix provider to apply to it.</param>
        public ApplicableComponentsAttribute(ProjectComponents components)
        {
            Components = components;
        }
    }
}
