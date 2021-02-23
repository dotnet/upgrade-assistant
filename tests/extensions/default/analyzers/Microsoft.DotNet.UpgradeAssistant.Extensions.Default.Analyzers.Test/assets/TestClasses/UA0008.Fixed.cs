using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace TestProject.TestClasses
{
    public class UA0008 : IUrlHelper
    {
        public IUrlHelper Method1(this IUrlHelper h)
        {
            var x = h;

            h.ExtenstionMethod(new TestProject.MyNamespace.UrlHelper(), new UrlHelper());

            UrlHelper.GenerateUrl(null, null, null, null, null, null, false);

            return h;
        }
    }
}
