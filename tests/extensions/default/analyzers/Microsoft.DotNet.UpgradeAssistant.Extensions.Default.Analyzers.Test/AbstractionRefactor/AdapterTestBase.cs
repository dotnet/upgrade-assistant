// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Test
{
    public abstract class AdapterTestBase
    {
        protected ICodeFixTest CreateTest(ICodeFixTest test, AdapterDescriptor? attributeDescriptor = null, bool withFix = true)
        {
            const string Attribute = @"
using System;

namespace Microsoft.CodeAnalysis
{
    public class AdapterDescriptor : Attribute
    {
        public AdapterDescriptor(Type interfaceType, Type original)
        {
        }
    }
}";

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
