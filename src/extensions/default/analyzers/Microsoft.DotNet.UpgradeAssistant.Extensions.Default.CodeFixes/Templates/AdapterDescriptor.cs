using System;
using Microsoft.CodeAnalysis.Refactoring;
#if NET || NETCOREAPP
using Microsoft.AspNetCore.Http;
#else
using System.Web;
#endif

[assembly: AdapterDescriptor(typeof(/*{{DEPRECATED_TYPE}}*/))]

namespace Microsoft.CodeAnalysis.Refactoring
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    internal sealed class AdapterDescriptorAttribute : Attribute
    {
        public AdapterDescriptorAttribute(Type original, Type interfaceType)
        {
        }
        public AdapterDescriptorAttribute(Type original)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    internal sealed class AdapterFactoryDescriptorAttribute : Attribute
    {
        public AdapterFactoryDescriptorAttribute(Type factoryType, string factoryMethod)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    internal class AdapterStaticDescriptorAttribute : Attribute
    {
        public AdapterStaticDescriptorAttribute(Type originalType, string originalString, Type destinationType, string destinationString)
        {
        }
    }
}
