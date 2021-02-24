// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public class UpgradeException : Exception
    {
        public UpgradeException()
        {
        }

        public UpgradeException(string message)
            : base(message)
        {
        }

        public UpgradeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
