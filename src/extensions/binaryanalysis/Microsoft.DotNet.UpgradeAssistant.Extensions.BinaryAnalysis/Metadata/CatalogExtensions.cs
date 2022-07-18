// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;

using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.ApiCatalog;

internal static class CatalogExtensions
{
    private static readonly SymbolDisplayFormat _nameFormat = new(
        memberOptions: SymbolDisplayMemberOptions.IncludeParameters,
        parameterOptions: SymbolDisplayParameterOptions.IncludeType,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);

    public static Guid GetCatalogGuid(this ISymbol symbol)
    {
        if (symbol is ITypeParameterSymbol)
        {
            return Guid.Empty;
        }

        if (symbol is INamespaceSymbol ns && ns.IsGlobalNamespace)
        {
            return GetCatalogGuid("N:<global>");
        }

        var id = symbol.OriginalDefinition.GetDocumentationCommentId();
        if (id == null)
        {
            return Guid.Empty;
        }

        return GetCatalogGuid(id);
    }

    public static Guid GetCatalogGuid(string packageId, string packageVersion)
    {
        return GetCatalogGuid($"{packageId}/{packageVersion}");
    }

    private static Guid GetCatalogGuid(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
#pragma warning disable CA5351 // Do Not Use Broken Cryptographic Algorithms
        using var md5 = MD5.Create();
#pragma warning restore CA5351 // Do Not Use Broken Cryptographic Algorithms
        var hashBytes = md5.ComputeHash(bytes);
        return new Guid(hashBytes);
    }

    public static string? GetCatalogName(this ISymbol symbol)
    {
        if (symbol is INamespaceSymbol)
        {
            return symbol.ToString();
        }

        if (symbol is INamedTypeSymbol type && type.IsTupleType)
        {
            var sb = new StringBuilder();
            sb.Append(type.Name);

            if (type.TypeParameters.Length > 0)
            {
                sb.Append('<');

                for (var i = 0; i < type.TypeParameters.Length; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(", ");
                    }

                    sb.Append(type.TypeParameters[i].Name);
                }

                sb.Append('>');
            }

            return sb.ToString();
        }

        return symbol!.ToDisplayString(_nameFormat);
    }

    public static bool IsAccessor(this ISymbol symbol)
    {
        return symbol is IMethodSymbol
        {
            MethodKind: MethodKind.PropertyGet or
            MethodKind.PropertySet or
            MethodKind.EventAdd or
            MethodKind.EventRemove or
            MethodKind.EventRaise
        };
    }

    public static ApiKind GetApiKind(this ISymbol symbol)
    {
        return symbol switch
        {
            INamespaceSymbol => ApiKind.Namespace,
            INamedTypeSymbol t => t.TypeKind switch
            {
                TypeKind.Interface => ApiKind.Interface,
                TypeKind.Delegate => ApiKind.Delegate,
                TypeKind.Enum => ApiKind.Enum,
                TypeKind.Struct => ApiKind.Struct,
                _ => ApiKind.Class
            },
            IMethodSymbol m => m.MethodKind switch
            {
                MethodKind.Constructor => ApiKind.Constructor,
                MethodKind.Destructor => ApiKind.Destructor,
                MethodKind.UserDefinedOperator => ApiKind.Operator,
                MethodKind.Conversion => ApiKind.Operator,
                MethodKind.PropertyGet => ApiKind.PropertyGetter,
                MethodKind.PropertySet => ApiKind.PropertySetter,
                MethodKind.EventAdd => ApiKind.EventAdder,
                MethodKind.EventRemove => ApiKind.EventRemover,
                MethodKind.EventRaise => ApiKind.EventRaiser,
                _ => ApiKind.Method
            },
            IFieldSymbol f when f.ContainingType.TypeKind == TypeKind.Enum => ApiKind.EnumItem,
            IFieldSymbol { IsConst: true } => ApiKind.Constant,
            IFieldSymbol => ApiKind.Field,
            IPropertySymbol => ApiKind.Property,
            IEventSymbol => ApiKind.Event,
            _ => throw new ArgumentException($"Unexpected symbol kind {symbol.Kind}")
        };
    }

    public static string GetPublicKeyTokenString(this AssemblyIdentity identity)
    {
        return BitConverter.ToString(identity.PublicKeyToken.ToArray()).Replace("-", string.Empty, StringComparison.InvariantCulture).ToLowerInvariant();
    }

    public static IEnumerable<INamedTypeSymbol> GetAllTypes(this IAssemblySymbol symbol)
    {
        var stack = new Stack<INamespaceSymbol>();
        stack.Push(symbol.GlobalNamespace);

        while (stack.Count > 0)
        {
            var ns = stack.Pop();
            foreach (var member in ns.GetMembers())
            {
                if (member is INamespaceSymbol childNs)
                {
                    stack.Push(childNs);
                }
                else if (member is INamedTypeSymbol type)
                {
                    yield return type;
                }
            }
        }
    }

    public static bool IsIncludedInCatalog(this ISymbol symbol)
    {
        if (symbol.DeclaredAccessibility != Accessibility.Public &&
            symbol.DeclaredAccessibility != Accessibility.Protected &&
            symbol.DeclaredAccessibility != Accessibility.ProtectedOrInternal)
        {
            return false;
        }

        if (symbol.ContainingType?.TypeKind == TypeKind.Delegate)
        {
            return false;
        }

        if (symbol is IMethodSymbol { MethodKind: MethodKind.Constructor, Parameters.Length: 0 } m && m.ContainingType.IsValueType)
        {
            return false;
        }

        return true;
    }

    public static bool IsIncludedInCatalog(this AttributeData attribute)
    {
        if (attribute.AttributeClass?.IsIncludedInCatalog() == false)
        {
            return false;
        }

        switch (attribute.AttributeClass?.Name)
        {
            case "CompilerGeneratedAttribute":
            case "TargetedPatchingOptOutAttribute":
            case "DynamicAttribute":
            case "TupleElementNamesAttribute":
                return false;
            default:
                return true;
        }
    }

    public static ImmutableArray<AttributeData> GetCatalogAttributes(this IMethodSymbol method)
    {
        if (method == null)
        {
            return ImmutableArray<AttributeData>.Empty;
        }

        return method.GetAttributes().Where(IsIncludedInCatalog).ToImmutableArray();
    }

    public static IEnumerable<ITypeSymbol> Ordered(this IEnumerable<ITypeSymbol> types)
    {
        var comparer = Comparer<ITypeSymbol>.Create((x, y) =>
        {
            if (x.Name == y.Name)
            {
                var xGenericArity = 0;
                var yGenericArity = 0;

                if (x is INamedTypeSymbol xNamed)
                {
                    xGenericArity = xNamed.TypeParameters.Length;
                }

                if (y is INamedTypeSymbol yNamed)
                {
                    yGenericArity = yNamed.TypeParameters.Length;
                }

                var result = xGenericArity.CompareTo(yGenericArity);
                if (result != 0)
                {
                    return result;
                }
            }

            return string.Compare(x.ToDisplayString(), y.ToDisplayString(), StringComparison.Ordinal);
        });

        return types.OrderBy(t => t, comparer);
    }

    public static IEnumerable<AttributeData> Ordered(this IEnumerable<AttributeData> attributes)
    {
        return attributes.OrderBy(a => a.AttributeClass?.Name)
            .ThenBy(a => a.ConstructorArguments.Length)
            .ThenBy(a => a.NamedArguments.Length);
    }

    public static IEnumerable<KeyValuePair<string, TypedConstant>> Ordered(this IEnumerable<KeyValuePair<string, TypedConstant>> namedArguments)
    {
        return namedArguments.OrderBy(kv => kv.Key);
    }

    public static string? GetNamedArgument(this AttributeData attribute, string name)
    {
        foreach (var (key, argument) in attribute.NamedArguments)
        {
            if (string.Equals(key, name, StringComparison.Ordinal) && argument.Value is string value)
            {
                return value;
            }
        }

        return null;
    }

    public static bool MatchesName(this INamedTypeSymbol symbol, string namespace1, string typeName)
    {
        return symbol is not null &&
               symbol.Name == typeName &&
               symbol.ContainingNamespace?.Name == namespace1 &&
               symbol.ContainingNamespace?.ContainingNamespace?.IsGlobalNamespace == true;
    }

    public static bool MatchesName(this INamedTypeSymbol symbol, string namespace1, string namespace2, string namespace3, string typeName)
    {
        return symbol is not null &&
               symbol.Name == typeName &&
               symbol.ContainingNamespace?.Name == namespace3 &&
               symbol.ContainingNamespace?.ContainingNamespace?.Name == namespace2 &&
               symbol.ContainingNamespace?.ContainingNamespace?.ContainingNamespace?.Name == namespace1 &&
               symbol.ContainingNamespace?.ContainingNamespace?.ContainingNamespace?.ContainingNamespace?.IsGlobalNamespace is true;
    }
}
