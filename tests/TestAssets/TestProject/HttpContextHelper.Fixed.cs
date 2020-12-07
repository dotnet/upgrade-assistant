using Microsoft.AspNetCore.Http;

namespace AspNetMigration
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
