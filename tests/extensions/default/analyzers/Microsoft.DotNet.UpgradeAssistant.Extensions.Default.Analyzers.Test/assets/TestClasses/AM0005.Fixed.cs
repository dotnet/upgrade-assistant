using System;
using System.Web;
using AspNetMigration;

namespace TestProject.TestClasses
{
    public class AM0005
    {
        public HttpContext GetContext()
        {
            var x = Foo.HttpContext.Current;
            return (HttpContext)x ?? HttpContextHelper.Current;
        }

        public void TestMethod()
        {
            var w = HttpContext.Current();
            var u = HttpContextHelper.Current;
            var y = HttpContextHelper.Current;
            Console.WriteLine(HttpContextHelper.Current);
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
