namespace System.Web
{
    public class HttpContext
    {
        public static HttpContext Current { get; }

        public static explicit operator HttpContext(Foo.HttpContext v)
        {
            throw new NotImplementedException();
        }
    }
}
