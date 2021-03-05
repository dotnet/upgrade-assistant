// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.DotNet.UpgradeAssistant.Portability.Service
{
    public class ApiDefinition
    {
        public string DocId { get; set; } = string.Empty;

        public string ReturnType { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string Parent { get; set; } = string.Empty;

        public override bool Equals(object? obj)
        {
            if (obj is not ApiDefinition other)
            {
                return false;
            }

            return string.Equals(DocId, other.DocId, StringComparison.Ordinal);
        }

        public override int GetHashCode()
            => DocId.GetHashCode();
    }
}
