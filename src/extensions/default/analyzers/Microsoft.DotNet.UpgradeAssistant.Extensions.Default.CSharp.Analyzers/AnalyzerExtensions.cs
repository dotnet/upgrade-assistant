// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CSharp.Analyzers
{
    public static class AnalyzerExtensions
    {
        public static bool IsCorrectLanguage(this DiagnosticAnalyzer analyzer, Compilation comp)
        {
            if (analyzer is null)
            {
                return false;
            }

            var langFromAttributes = GetSpecifiedLanguage(analyzer.GetType());
            if (!langFromAttributes.Any())
            {
                // this analyzer is language agnostic because it does not specify an attribute
                return true;
            }

            return langFromAttributes.Any(lang => lang.Equals(comp.Language, StringComparison.Ordinal));
        }

        /// <summary>
        /// Review the Attributes of the <paramref name="analyzer"/> to determine
        /// if which languages apply.
        /// </summary>
        /// <param name="analyzer">True</param>
        /// <returns>A list of supported languages.</returns>
        private static IEnumerable<string> GetSpecifiedLanguage(Type analyzer)
        {
            var analyzerAttr = analyzer.CustomAttributes.FirstOrDefault(a => a.AttributeType.FullName.Equals(typeof(DiagnosticAnalyzerAttribute).FullName, StringComparison.Ordinal));
            if (analyzerAttr is null)
            {
                // this object is not the type we thought it was, the filter does not apply
                return Enumerable.Empty<string>();
            }

            var supportedLanguages = new List<string>();
            var firstLanguage = analyzerAttr.ConstructorArguments.FirstOrDefault().Value as string;
            if (firstLanguage is null)
            {
                // this object is not the type we thought it was, the filter does not apply
                return Enumerable.Empty<string>();
            }

            supportedLanguages.Add(firstLanguage);

            var additionalLanguages = analyzerAttr.ConstructorArguments.LastOrDefault().Value as IEnumerable<System.Reflection.CustomAttributeTypedArgument>;
            if (additionalLanguages is not null)
            {
                supportedLanguages.AddRange(additionalLanguages.Select(l => l.Value.ToString()));
            }

            return supportedLanguages;
        }
    }
}
