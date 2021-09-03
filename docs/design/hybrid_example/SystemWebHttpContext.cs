#if NETFRAMEWORK
using System.Collections.Specialized;
using System.Web;

namespace HybridExample
{
    internal class SystemWebHttpContext : IHttpContext, IRequest, IResponse
    {
        private readonly HttpContext _context;

        public SystemWebHttpContext(HttpContext context)
        {
            _context = context;
        }

        public IRequest Request => this;

        public IResponse Response => this;

        IHeaders IRequest.Headers => new Headers(_context.Request.Headers);

        private class Headers : IHeaders
        {
            private readonly NameValueCollection _collection;

            public Headers(NameValueCollection collection)
            {
                _collection = collection;
            }

            public string this[string name] => _collection[name];
        }
    }
}
#endif
