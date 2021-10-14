using System;
using HybridExample;
using Microsoft.CodeAnalysis.Refactoring;

#if NETFRAMEWORK
[assembly: AdapterDescriptor(typeof(IHttpContext), typeof(System.Web.HttpContext))]
[assembly: AdapterDescriptor(typeof(IHttpContext), typeof(System.Web.HttpContextBase))]
[assembly: AdapterDescriptor(typeof(IRequest), typeof(System.Web.HttpRequest))]
[assembly: AdapterDescriptor(typeof(IResponse), typeof(System.Web.HttpResponse))]
#elif NETCOREAPP
[assembly: AdapterDescriptor(typeof(IHttpContext), typeof(Microsoft.AspNetCore.Http.HttpContext))]
[assembly: AdapterDescriptor(typeof(IRequest), typeof(Microsoft.AspNetCore.Http.HttpRequest))]
[assembly: AdapterDescriptor(typeof(IResponse), typeof(Microsoft.AspNetCore.Http.HttpResponse))]
#endif


namespace Microsoft.CodeAnalysis.Refactoring
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    internal sealed class AdapterDescriptorAttribute : Attribute
    {
        public AdapterDescriptorAttribute(Type interfaceType, Type original)
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
