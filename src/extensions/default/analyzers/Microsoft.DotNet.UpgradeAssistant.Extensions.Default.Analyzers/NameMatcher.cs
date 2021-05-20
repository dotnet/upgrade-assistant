using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default
{
    public class NameMatcher
    {
        public static NameMatcher HttpContextCurrent { get; } = new NameMatcher("System.Web.HttpContext", "Current");

        private readonly string[] _typeName;
        private readonly string? _propertyName;

        public NameMatcher(string typeName, string? propertyName = null)
        {
            if (typeName is null)
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            _typeName = typeName.Split('.');
            _propertyName = propertyName;
        }

        public string TypeName => _typeName[_typeName.Length - 1];

        public bool Matches(IPropertySymbol property)
        {
            if (property is null)
            {
                return false;
            }

            if (!property.Name.Equals(_propertyName, StringComparison.Ordinal))
            {
                return false;
            }

            return Matches(property.Type);
        }

        public bool Matches(ITypeSymbol? typeSymbol)
        {
            if (typeSymbol is null)
            {
                return false;
            }

            var symbol = (INamespaceOrTypeSymbol)typeSymbol;

            for (int i = _typeName.Length - 1; i >= 0; i--)
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
