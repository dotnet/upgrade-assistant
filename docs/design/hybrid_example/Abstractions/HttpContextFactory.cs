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
        private const string Key = "HttpContextAdapter";

        public static IHttpContext Create(HttpContext context) 
        {
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
