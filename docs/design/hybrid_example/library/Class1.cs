using System.Web;

namespace HybridExample
{
    public class Class1
    {
        public string Method(HttpContext context)
        {
            return Method2(context.Request);
        }

        public string Method2(HttpRequest request)
        {
            request.Abort();
            return request.Headers["headerName"];
        }
    }
}
