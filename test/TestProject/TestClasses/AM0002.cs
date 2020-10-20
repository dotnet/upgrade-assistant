using System;
using System.Web;

namespace TestProject.TestClasses
{
    public class AM0002
    {
        public IHtmlString String1 { get; set; }
        public HtmlString String2 { get; set; }
        public Microsoft.AspNetCore.Html.HtmlString String2A { get; set; }
        public System.Web.Mvc.MvcHtmlString String3 { get; set; }
        public MvcHtmlString String4 { get; set; }
        public MyNamespace.MvcHtmlString String5 { get; set; }
        public String String6 { get; set; }
    }
}
