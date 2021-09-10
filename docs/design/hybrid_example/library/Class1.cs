using System.Web;

namespace hybrid_example
{
    public class Class1
    {
        public string Method(HttpContext context)
        {
            return Method2(context.Request);
        }

        public string Method2(HttpRequest request)
        {
            return request.Headers["headerName"];
        }
    }
}
