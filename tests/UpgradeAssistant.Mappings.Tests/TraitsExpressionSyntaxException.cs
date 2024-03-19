// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.UpgradeAssistant.Mappings.Tests;

internal class TraitsExpressionSyntaxException : FormatException
{
    public TraitsExpressionSyntaxException(string message)
        : base(message)
    {
    }
}
