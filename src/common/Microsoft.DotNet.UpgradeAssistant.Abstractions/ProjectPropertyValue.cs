// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public record ProjectPropertyValue
    {
        /// <summary>
        /// Gets the name and value of unevalualted properties in csproj.
        /// </summary>
        public string? PropertyName { get; init; }

        public string? PropertyValue { get; init; }

        public ProjectPropertyValue(string name, string value)
        {
            PropertyName = name;
            PropertyValue = value;
        }
    }
}
