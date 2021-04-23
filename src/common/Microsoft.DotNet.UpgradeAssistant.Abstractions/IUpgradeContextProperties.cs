// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.UpgradeAssistant
{
    /// <summary>
    /// Manages properties related to the upgrade context.
    /// </summary>
    public interface IUpgradeContextProperties
    {
        /// <summary>
        /// Returns a property value stored in the context.
        /// </summary>
        /// <param name="propertyName">Name identifier for the property.</param>
        /// <returns>The property value.</returns>
        string? GetPropertyValue(string propertyName);

        /// <summary>
        /// Stores a property value to the context.
        /// </summary>
        /// <param name="propertyName">Name identifier for the property.</param>
        /// <param name="value">The property value to store.</param>
        /// <param name="persistent">Whether to persist the value when the program terminates.</param>
        void SetPropertyValue(string propertyName, string value, bool persistent);

        /// <summary>
        /// Removes a property value by name.
        /// </summary>
        /// <param name="propertyName">Name identifier for the property.</param>
        /// <returns>true if the element is successfully found and removed; otherwise, false.</returns>
        bool RemovePropertyValue(string propertyName);

        /// <summary>
        /// Gets property values marked persistent.
        /// </summary>
        /// <returns>A list of property keys and values.</returns>
        IEnumerable<KeyValuePair<string, string>> GetAllPropertyValues();

        /// <summary>
        /// Gets property values marked persistent.
        /// </summary>
        /// <returns>A list of property keys and values.</returns>
        IEnumerable<KeyValuePair<string, string>> GetPersistentPropertyValues();
    }
}
