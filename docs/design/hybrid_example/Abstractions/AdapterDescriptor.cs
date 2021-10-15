using System;
using HybridExample;
using Microsoft.CodeAnalysis.Refactoring;

#if NETFRAMEWORK
[assembly: AdapterDescriptor(typeof(System.Web.HttpContext), typeof(IHttpContext))]
[assembly: AdapterDescriptor(typeof(System.Web.HttpContextBase), typeof(IHttpContext))]
[assembly: AdapterDescriptor(typeof(System.Web.HttpRequest), typeof(IRequest))]
[assembly: AdapterDescriptor(typeof(System.Web.HttpResponse), typeof(IResponse))]
#elif NETCOREAPP
[assembly: AdapterDescriptor(typeof(Microsoft.AspNetCore.Http.HttpContext), typeof(IHttpContext))]
[assembly: AdapterDescriptor(typeof(Microsoft.AspNetCore.Http.HttpRequest), typeof(IRequest))]
[assembly: AdapterDescriptor(typeof(Microsoft.AspNetCore.Http.HttpResponse), typeof(IResponse))]
#endif


namespace Microsoft.CodeAnalysis.Refactoring
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    internal sealed class AdapterDescriptorAttribute : Attribute
    {
        public AdapterDescriptorAttribute(Type original, Type? interfaceType)
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
}
