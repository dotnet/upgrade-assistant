// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.UpgradeAssistant.Mappings.Tests;

internal class ApiMapEntry
{
    public string? Value { get; set; }

    public string Kind { get; set; } = Kinds.Type;

    public string State { get; set; } = States.Replaced;

    public bool NeedsManualUpgrade { get; set; }

    public string? DocumentationUrl { get; set; }

    public bool NeedsTodoInComment { get; set; } = true;

    public bool IsAsync { get; set; }

    /// <summary>
    ///  Gets or sets whether the API represents an extension method.
    /// </summary>
    /// <remarks>
    ///  Although extension methods can only be static, this setting is disconnected from <see cref="IsStatic"/>.
    /// </remarks>
    public bool IsExtension { get; set; }

    public bool IsStatic { get; set; }

    public string? MessageId { get; set; }

    public string[]? MessageParams { get; set; }

    public static class Kinds
    {
        public const string Property = nameof(Property);
        public const string Method = nameof(Method);
        public const string Namespace = nameof(Namespace);
        public const string Type = nameof(Type);
    }

    public static class States
    {
        public const string NotImplemented = nameof(NotImplemented);
        public const string Removed = nameof(Removed);
        public const string Replaced = nameof(Replaced);
    }
}
