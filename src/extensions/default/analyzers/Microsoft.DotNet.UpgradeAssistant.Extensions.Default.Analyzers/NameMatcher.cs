using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default
{
    public class NameMatcher
    {
        private readonly string[] _typeName;
        private readonly string? _memberName;

        private NameMatcher(string typeName, string? memberName = null)
        {
            if (typeName is null)
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            _typeName = typeName.Split('.');
            _memberName = memberName;
        }

        public static NameMatcher MatchType(string typeName) => new NameMatcher(typeName);

        public static NameMatcher MatchPropertyAccess(string typeName, string memberName) => new(typeName, memberName);

        public string TypeName => _typeName[_typeName.Length - 1];

        public bool MatchesPartiallyQualifiedType(string partiallyQualifiedTypeName)
        {
            if (partiallyQualifiedTypeName is null)
            {
                throw new ArgumentNullException(nameof(partiallyQualifiedTypeName));
            }

            var partialName = partiallyQualifiedTypeName.Split('.');

            if (partialName.Length > _typeName.Length)
            {
                return false;
            }

            for (var i = 1; i <= partialName.Length; i++)
            {
                if (!partialName[partialName.Length - i].Equals(_typeName[_typeName.Length - i], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        public bool Matches(IPropertySymbol property)
        {
            if (property is null)
            {
                return false;
            }

            if (!property.Name.Equals(_memberName, StringComparison.Ordinal))
            {
                return false;
            }

            return Matches(property.Type);
        }

        public bool MatchesConstructorOfType(ISymbol theSymbol)
        {
            if (theSymbol is null || theSymbol is not IMethodSymbol)
            {
                return false;
            }

            var methodSymbol = (IMethodSymbol)theSymbol;
            return methodSymbol.MethodKind == MethodKind.Constructor && Matches(methodSymbol.ContainingType);
        }

        public bool Matches(ITypeSymbol? typeSymbol)
        {
            // Cast here because its parent will be a namespace or type, and we want to only take in a type and not namespace
            var symbol = (INamespaceOrTypeSymbol?)typeSymbol;

            for (var i = _typeName.Length - 1; i >= 0; i--)
            {
                if (symbol is null)
                {
                    return false;
                }

                if (!string.Equals(symbol.Name, _typeName[i], StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                symbol = symbol.ContainingNamespace;
            }

            return symbol is INamespaceSymbol ns && ns.IsGlobalNamespace;
        }
    }
}
