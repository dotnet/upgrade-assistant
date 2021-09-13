using System.Web;

namespace hybrid_example
{
    public class Class1
    {
        public string Method(HybridExample.IHttpContext context)
        {
            return Method2(context.Request);
        }

        public string Method2(HybridExample.IRequest request)
        {
            return request.Headers["headerName"];
        }
    }
}
