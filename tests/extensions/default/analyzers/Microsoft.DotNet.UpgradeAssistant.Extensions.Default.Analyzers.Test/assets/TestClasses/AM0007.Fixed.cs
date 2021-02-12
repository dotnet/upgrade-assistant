using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace TestProject.TestClasses
{
    public class AM0007 : IHtmlHelper
    {
        public AM0007()
        {
        }

        // General usage => IHtmlHelper
        public static TestProject.MyNamespace.HtmlHelper Method1(this IHtmlHelper helper)
        {
            List<IHtmlHelper> x = new List<IHtmlHelper>();

            return new TestProject.MyNamespace.HtmlHelper();
        }

        // Member access and ctor => HtmlHelper
        public static IEnumerable<IHtmlHelper> Method2(IHtmlHelper helper)
        {
            HtmlHelper.ClientValidationEnabled = true;
            Foo(HtmlHelper.AnonymousObjectToHtmlAttributes(null));
            var x = new HtmlHelper(null, null);
            x = new HtmlHelper();
            helper.ExtensionMethod();
            yield return x;
        }
    }
}
