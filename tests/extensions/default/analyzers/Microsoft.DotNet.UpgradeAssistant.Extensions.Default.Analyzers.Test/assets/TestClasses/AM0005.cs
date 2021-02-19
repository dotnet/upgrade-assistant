using System;
using System.Web;

namespace TestProject.TestClasses
{
    public class AM0005
    {
        public HttpContext GetContext()
        {
            var x = Foo.HttpContext.Current;
            return (HttpContext)x ?? HttpContext.Current;
        }

        public void TestMethod()
        {
            var w = HttpContext.Current();
            var u = Bar.HttpContext.Current;
            var y = System.Web.HttpContext.Current;
            Console.WriteLine(HttpContext.Current);
        }
    }
}

namespace Foo
{
    public class HttpContext
    {
        public static HttpContext Current { get; }
    }
}
