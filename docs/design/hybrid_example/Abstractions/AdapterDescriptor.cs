using System;
using HybridExample;

[assembly: AdapterDescriptor(false)]

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


namespace HybridExample
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    internal sealed class AdapterDescriptor : Attribute
    {
        public AdapterDescriptor(Type interfaceType, Type original)
        {
        }
        public AdapterDescriptor(bool isEnabled)
        {
        }
    }
}
