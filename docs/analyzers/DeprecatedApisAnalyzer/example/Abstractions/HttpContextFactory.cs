using System;

#if NETFRAMEWORK
using System.Web;
#else
using Microsoft.AspNetCore.Http;
#endif

[assembly: Microsoft.CodeAnalysis.Refactoring.AdapterFactoryDescriptor(typeof(HybridExample.HttpContextFactory), nameof(HybridExample.HttpContextFactory.Create))]

namespace HybridExample
{
    public static class HttpContextFactory
    {
        private const string Message = "Prefer accessing HttpContext via injection";
        private const string Key = "HttpContextAdapter";

#if NET || NETCOREAPP
#if NET5_0_OR_GREATER
        [Obsolete(Message, error: false, DiagnosticId = "HttpContextCurrent", UrlFormat = "https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-context")]
#else
        [Obsolete(Message, error: false)]
#endif
        public static IHttpContext? Current => Create(HttpContextAccessor.HttpContext);

        private static readonly HttpContextAccessor HttpContextAccessor = new();
#else
#pragma warning disable UA0005 // Do not use HttpContext.Current
        [Obsolete(Message, error: false)]
        public static IHttpContext? Current => Create(HttpContext.Current);
#pragma warning restore UA0005 // Do not use HttpContext.Current
#endif

#if NET5_0_OR_GREATER
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull("context")]
#endif
        public static IHttpContext? Create(HttpContext? context)
        {
            if (context is null)
            {
                return null;
            }

#if NETFRAMEWORK
            if (context.Items.Contains(Key) && context.Items[Key] is IHttpContext icontext)
            {
                return icontext;
            }

            var created = new SystemWebHttpContext(context);
#else
            if (context.Items.TryGetValue(Key, out var result) && result is IHttpContext icontext)
            {
                return icontext;
            }

            var created = new AspNetHttpContext(context);
#endif

            context.Items.Add(Key, created);

            return created;

        }
    }
}
