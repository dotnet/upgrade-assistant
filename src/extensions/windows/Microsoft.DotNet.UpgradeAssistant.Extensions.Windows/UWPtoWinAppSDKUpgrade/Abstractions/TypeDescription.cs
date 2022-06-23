// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows.UWPtoWinAppSDKUpgrade.Abstractions
{
    internal class TypeDescription : IApiDescription
    {
        public TypeDescription(string @namespace, string typeName)
        {
            Namespace = @namespace;
            TypeName = typeName;
        }

        public string Namespace { get; init; }

        public string TypeName { get; init; }

        public ApiType ApiType => ApiType.TypeApi;
    }
}
