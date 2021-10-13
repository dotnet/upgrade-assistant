// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Test
{
    public abstract class AdapterTestBase
    {
        protected const string Attribute = @"
using System;

namespace Microsoft.CodeAnalysis
{
    public class AdapterDescriptor : Attribute
    {
        public AdapterDescriptor(Type interfaceType, Type original)
        {
        }
    }

    public class FactoryDescriptor : Attribute
    {
        public FactoryDescriptor(Type factoryClass, string factoryMethod)
        {
        }
    }
}";

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
