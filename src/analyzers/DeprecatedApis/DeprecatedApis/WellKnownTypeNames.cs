// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.UpgradeAssistant.DeprecatedApisAnalyzer
{
    public static class WellKnownTypeNames
    {
        private const string Period = ".";

        public const string AttributeNamespace = "Microsoft.CodeAnalysis.Refactoring";
        public const string AdapterDescriptor = "AdapterDescriptorAttribute";
        public const string AdapterDescriptorFullyQualified = AttributeNamespace + Period + AdapterDescriptor;
        public const string AdapterStaticDescriptor = "AdapterStaticDescriptorAttribute";
        public const string AdapterStaticDescriptorFullyQualified = AttributeNamespace + Period + AdapterStaticDescriptor;
        public const string FactoryDescriptor = "AdapterFactoryDescriptorAttribute";
        public const string FactoryDescriptorFullyQualified = AttributeNamespace + Period + FactoryDescriptor;
    }
}
