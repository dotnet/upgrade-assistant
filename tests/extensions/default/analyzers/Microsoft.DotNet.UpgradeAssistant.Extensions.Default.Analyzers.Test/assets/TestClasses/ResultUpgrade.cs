using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TestProject.TestClasses
{
    public class ResultUpgrade
    {
        public System.Web.Mvc.ViewResult Index()
        {
            return View();
        }

        public Foo.RedirectResult AnotherPage() => new RedirectResult("foo");

        public ActionResult YetAnotherPage()
        {
            System.Web.Mvc.ActionResult a = new System.Web.Mvc.ViewResult();
            a = new RedirectResult("bar");

            return new HttpNotFoundResult();
        }

        // These shouldn't be flagged by analyzers
        public Microsoft.AspNetCore.Mvc.ActionResult OneMoreAction()
        {
            return new Microsoft.AspNetCore.Mvc.ViewResult();
        }
    }
}
