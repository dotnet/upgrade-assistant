#if NET6_0
using Microsoft.AspNetCore.Http;

namespace HybridExample
{
    internal class AspNetHttpContext : IHttpContext, IRequest, IResponse
    {
        private readonly HttpContext _context;

        public AspNetHttpContext(HttpContext context)
        {
            _context = context;
        }

        public IRequest Request => this;

        public IResponse Response => this;

        IHeaders IRequest.Headers => new Headers(_context.Request.Headers);

        private class Headers : IHeaders
        {
            private readonly IHeaderDictionary _collection;

            public Headers(IHeaderDictionary collection)
            {
                _collection = collection;
            }

            public string this[string name] => _collection[name];
        }
    }
}
#endif
