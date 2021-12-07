using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TestProject.TestClasses
{
    public class ResultUpgrade
    {
        public ViewResult Index()
        {
            return View();
        }

        public Foo.RedirectResult AnotherPage() => new RedirectResult("foo");

        public ActionResult YetAnotherPage()
        {
            ActionResult a = new ViewResult();
            a = new RedirectResult("bar");

            return new NotFoundResult();
        }

        // These shouldn't be flagged by analyzers
        public Microsoft.AspNetCore.Mvc.ActionResult OneMoreAction()
        {
            return new Microsoft.AspNetCore.Mvc.ViewResult();
        }
    }
}
