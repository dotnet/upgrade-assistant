// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.UpgradeAssistant.Mappings.Tests;

internal class TraitToken
{
    private static readonly char[] DotSeparator = new[] { '.' };

    public static string? GetPropertyName(TraitToken token, string id)
    {
        if (!string.Equals(token.TraitName, id, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return token.PropertyName;
    }

    public const string ColonOperator = ":";
    public const string LessOperator = "<";
    public const string MoreOperator = ">";
    public const string EqualsOperator = "=";
    public const string MoreOrEqualsOperator = ">=";
    public const string LessOrEqualsOperator = "<=";

    private static readonly HashSet<string> Operators = new(StringComparer.Ordinal)
    {
        ColonOperator,
        LessOperator,
        MoreOperator,
        EqualsOperator,
        MoreOrEqualsOperator,
        LessOrEqualsOperator
    };

    private static readonly char[] OperatorSymbols = ":<>=!".ToCharArray();

    public static bool TryParse(string input, out TraitToken? token)
    {
        token = null;

        if (string.IsNullOrEmpty(input))
        {
            return false;
        }

        int i = 0;
        int begin = i;

        while (i < input.Length && !IsOperatorCharacter(input[i])) { i++; }

        if (i == input.Length)
        {
            return false;
        }

        string key = input.Substring(begin, i).Trim();
        if (string.IsNullOrEmpty(key))
        {
            return false;
        }

        begin = i;

        while (i < input.Length && IsOperatorCharacter(input[i])) { i++; }

        string @operator = input.Substring(begin, i - begin);
        if (!Operators.Contains(@operator))
        {
            return false;
        }

        string value = i < input.Length ? input.Substring(i) : string.Empty;

        token = new TraitToken(key, value.Trim(), @operator);

        return true;
    }

    private static bool IsOperatorCharacter(char ch)
    {
        return Array.IndexOf(OperatorSymbols, ch) >= 0;
    }

    public TraitToken(string key, string value, string @operator)
    {
        Key = key;
        Value = value;
        Operator = @operator;
    }

    public string Key { get; }

    public string Value { get; }

    public string Operator { get; }

    private string? _traitName;
    public string? TraitName
    {
        get
        {
            if (string.IsNullOrEmpty(Key))
            {
                return null;
            }

            if (_traitName is null)
            {
                var parts = Key.Trim().Split(DotSeparator, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 1 || parts.Length > 2)
                {
                    return null;
                }

                _traitName = parts[0];
                _propertyName = parts.Length == 2 ? parts[1] : null;
            }

            return _traitName;
        }
    }

    private string? _propertyName;
    public string? PropertyName
    {
        get
        {
            if (string.IsNullOrEmpty(Key))
            {
                return null;
            }

            if (_propertyName is null)
            {
                var parts = Key.Trim().Split(DotSeparator, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 1 || parts.Length > 2)
                {
                    return null;
                }

                _traitName = parts[0];
                _propertyName = parts.Length == 2 ? parts[1] : null;
            }

            return _propertyName;
        }
    }

    public override string ToString()
    {
        return $"{{{Key}{Operator}{Value}}}";
    }
}
