// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.ApiCatalog;

public static class ApiKindExtensions
{
    public static bool IsType(this ApiKind kind)
    {
        switch (kind)
        {
            case ApiKind.Interface:
            case ApiKind.Delegate:
            case ApiKind.Enum:
            case ApiKind.Struct:
            case ApiKind.Class:
                return true;
            default:
                return false;
        }
    }

    public static bool IsMember(this ApiKind kind)
    {
        switch (kind)
        {
            case ApiKind.Field:
            case ApiKind.EnumItem:
            case ApiKind.Constant:
            case ApiKind.Constructor:
            case ApiKind.Destructor:
            case ApiKind.Operator:
            case ApiKind.Property:
            case ApiKind.PropertyGetter:
            case ApiKind.PropertySetter:
            case ApiKind.Method:
            case ApiKind.Event:
            case ApiKind.EventAdder:
            case ApiKind.EventRemover:
            case ApiKind.EventRaiser:
                return true;
            default:
                return false;
        }
    }

    public static bool IsAccessor(this ApiKind kind)
    {
        switch (kind)
        {
            case ApiKind.PropertyGetter:
            case ApiKind.PropertySetter:
            case ApiKind.EventAdder:
            case ApiKind.EventRemover:
            case ApiKind.EventRaiser:
                return true;
            default:
                return false;
        }
    }
}
