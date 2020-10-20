using System;
using System.Web;
using Microsoft.AspNetCore.Html;

namespace TestProject.TestClasses
{
    public class AM0002
    {
        public HtmlString String1 { get; set; }
        public HtmlString String2 { get; set; }
        public Microsoft.AspNetCore.Html.HtmlString String2A { get; set; }
        public HtmlString String3 { get; set; } = new /* Comment */ HtmlString/*Comment 2*/ (string.Empty);
        public HtmlString String4 { get; set; }
        public HtmlString[] String4A { get; set; }
        public MyNamespace.MvcHtmlString String5 { get; set; } = new MyNamespace.MvcHtmlString(string.Empty);
        public MyNamespace.MvcHtmlString[] String5A { get; set; }
        public String String6 { get; set; }
    }
}
