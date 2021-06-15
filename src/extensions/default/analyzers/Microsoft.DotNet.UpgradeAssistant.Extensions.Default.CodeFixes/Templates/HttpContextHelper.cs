using System;

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// Temporary helper class for retrieving the current <see cref="HttpContext"/> . This temporary
    /// workaround should be removed in the future and <see cref="HttpContext"/> HttpContext should be retrieved
    /// from the current controller, middleware, or page instead. If working in another
    /// component, the current <see cref="HttpContext"/> can be retrieved from an <see cref="IHttpContextAccessor"/>
    /// retrieved via dependency injection.
    /// </summary>
    internal static class HttpContextHelper
    {
        private static readonly HttpContextAccessor HttpContextAccessor = new HttpContextAccessor();

        /// <summary>
        /// Gets the current <see cref="HttpContext"/>. Returns <c>null</c> if there is no current <see cref="HttpContext"/>.
        /// </summary>
#if NET5_0_OR_GREATER
        [Obsolete("Prefer accessing HttpContext via injection", error: false, DiagnosticId = "HttpContextCurrent", UrlFormat = "https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-context")]
#endif
        public static HttpContext Current => HttpContextAccessor.HttpContext;
    }
}
