// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public class MigrationException : Exception
    {
        public MigrationException()
        {
        }

        public MigrationException(string message)
            : base(message)
        {
        }

        public MigrationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
