using System;
using System.Web.Mvc;

namespace TestProject.TestClasses
{
    public class AM0004A : IResultFilter
    {
        public void OnResultExecuted(ResultExecutedContext filterContext)
        {
            throw new NotImplementedException();
        }

        public void OnResultExecuting(ResultExecutingContext filterContext)
        {
            Foo.ResultExecutingContext x = (Foo.ResultExecutingContext)new Bar.ResultExecutingContext();
            throw new NotImplementedException();
        }
    }

    public class AM0004B : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            Microsoft.AspNetCore.Mvc.Filters.ActionExecutedContext a = null;
            var x = this as IActionFilter;
        }

        public override void OnActionExecuted(ActionExecutedContext filterContext) => throw new NotImplementedException();
    }
}
