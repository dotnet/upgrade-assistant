// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows.UWPtoWinAppSDKUpgrade.Abstractions
{
    internal class MethodDescription : IMemberDescription
    {
        public MethodDescription(TypeDescription typeDescription, string methodName, bool isStatic, bool isAsync)
        {
            TypeDescription = typeDescription;
            MemberName = methodName;
            IsStatic = isStatic;
            IsAsync = isAsync;
        }

        public ApiType ApiType => ApiType.MethodApi;

        public TypeDescription TypeDescription { get; init; }

        public string MemberName { get; init; }

        public bool IsStatic { get; init; }

        public bool IsAsync { get; init; }
    }
}
