using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.AspNetCore.Mvc;

namespace TestProject.TestClasses
{
    public class UA0003
    {
        public ViewResult Index()
        {
            return View();
        }

        public RedirectResult AnotherPage() => new RedirectResult("foo");

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
