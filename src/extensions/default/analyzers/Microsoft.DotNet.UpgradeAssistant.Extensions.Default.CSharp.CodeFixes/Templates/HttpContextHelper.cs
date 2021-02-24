using Microsoft.AspNetCore.Http;

namespace AlloyTemplates
{
    /// <summary>
    /// Temporary helper class for retrieving the current HttpContext. This temporary
    /// workaround should be removed in the future and HttpContext should be retrieved
    /// from the current controller, middleware, or page instead. If working in another
    /// component, the current HttpContext can be retrieved from an IHttpContextAccessor
    /// retrieved via dependency injection.
    /// </summary>
    public static class HttpContextHelper
    {
        private static IHttpContextAccessor HttpContextAccessor;

        /// <summary>
        /// Prepare HttpContextHelper by supplying it with an instance of IHttpContextAccessor.
        /// </summary>
        public static void Initialize(IHttpContextAccessor httpContextAccessor)
        {
            HttpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Gets the current HttpContext. Returns null if there is no current HttpContext.
        /// </summary>
        public static HttpContext Current => HttpContextAccessor?.HttpContext;
    }
}
