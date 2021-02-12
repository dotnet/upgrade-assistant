using System;
using System.Web;

namespace TestProject.TestClasses
{
    public class AM0002
    {
        public IHtmlString String1 { get; set; }
        public HtmlString String2 { get; set; }
        public Microsoft.AspNetCore.Html.HtmlString String2A { get; set; }
        public System.Web.HtmlString String3 { get; set; } = new /* Comment */ System.Web.Mvc.MvcHtmlString/*Comment 2*/ (string.Empty);
        public MvcHtmlString String4 { get; set; }
        public MvcHtmlString[] String4A { get; set; }
        public MyNamespace.MvcHtmlString String5 { get; set; } = new MyNamespace.MvcHtmlString(string.Empty);
        public MyNamespace.MvcHtmlString[] String5A { get; set; }
        public String String6 { get; set; }
    }
}
