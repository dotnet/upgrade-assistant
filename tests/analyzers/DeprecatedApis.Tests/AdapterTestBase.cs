// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.UpgradeAssistant.DeprecatedApisAnalyzer.Test
{
    public abstract class AdapterTestBase
    {
        protected const string Attribute = @$"
using System;

namespace {WellKnownTypeNames.AttributeNamespace}
{{
    public class {WellKnownTypeNames.AdapterStaticDescriptor} : Attribute
    {{
        public {WellKnownTypeNames.AdapterStaticDescriptor}(Type originalType, string originalString, Type destinationType, string destinationString)
        {{
        }}
    }}

    public class {WellKnownTypeNames.AdapterDescriptor} : Attribute
    {{
        public {WellKnownTypeNames.AdapterDescriptor}(Type original, Type interfaceType = null)
        {{
        }}
    }}

    public class {WellKnownTypeNames.FactoryDescriptor} : Attribute
    {{
        public {WellKnownTypeNames.FactoryDescriptor}(Type factoryClass, string factoryMethod)
        {{
        }}
    }}
}}";

        protected static ICodeFixTest CreateTest(ICodeFixTest test, AdapterDescriptorFactory? attributeDescriptor = null, bool withFix = true)
        {
            test.WithSource(Attribute);

            if (withFix)
            {
                test.WithFixed(Attribute);
            }

            if (attributeDescriptor is not null)
            {
                var descriptor = attributeDescriptor.CreateAttributeString(LanguageNames.CSharp);

                test.WithSource(descriptor);

                if (withFix)
                {
                    test.WithFixed(descriptor);
                }
            }

            return test;
        }
    }
}
