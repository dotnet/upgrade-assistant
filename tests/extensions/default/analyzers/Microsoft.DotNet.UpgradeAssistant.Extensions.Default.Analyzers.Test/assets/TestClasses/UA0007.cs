using System;
using System.Collections.Generic;

namespace TestProject.TestClasses
{
    public class UA0007 : System.Web.Mvc.HtmlHelper
    {
        public UA0007()
        {
        }

        // General usage => IHtmlHelper
        public static TestProject.MyNamespace.HtmlHelper Method1(this Foo.HtmlHelper helper)
        {
            List<System.Web.Mvc.HtmlHelper> x = new List<System.Web.Mvc.HtmlHelper>();

            return new TestProject.MyNamespace.HtmlHelper();
        }

        // Member access and ctor => HtmlHelper
        public static IEnumerable<Foo.HtmlHelper> Method2(HtmlHelper helper)
        {
            System.Web.Mvc.HtmlHelper.ClientValidationEnabled = true;
            Foo(HtmlHelper.AnonymousObjectToHtmlAttributes(null));
            var x = new System.Web.Mvc.HtmlHelper(null, null);
            x = new HtmlHelper();
            helper.ExtensionMethod();
            yield return x;
        }
    }
}
