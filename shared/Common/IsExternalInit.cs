﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#if !NET5_0_OR_GREATER

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Workaround to enable init properties and record types while targeting netstandard2.0.
    /// </summary>
    internal static class IsExternalInit
    {
    }
}

#endif
