using Microsoft.AspNetCore.Http;

namespace AspNetUpgrade
{
    public static class HttpContextHelper
    {
        private static IHttpContextAccessor httpContextAccessor;

        public static void Initialize(IHttpContextAccessor httpContextAccessor)
        {
        }

        public static HttpContext Current => httpContextAccessor?.HttpContext;
    }
}
