using System;

namespace TestProject.TestClasses
{
    public class FilterUpgrade : System.Web.Mvc.IResultFilter
    {
        public void OnResultExecuted(System.Web.Mvc.ResultExecutedContext filterContext)
        {
            throw new NotImplementedException();
        }

        public void OnResultExecuting(System.Web.Mvc.ResultExecutingContext filterContext)
        {
            Foo.ResultExecutingContext x = (Foo.ResultExecutingContext)new TestProject.MyNamespace.ResultExecutingContext();
            throw new NotImplementedException();
        }
    }

    public class UA0004B : System.Web.Mvc.ActionFilterAttribute
    {
        public override void OnActionExecuting(System.Web.Mvc.ActionExecutingContext context)
        {
            Microsoft.AspNetCore.Mvc.Filters.ActionExecutedContext a = null;
            var x = this as IActionFilter;
        }

        public override void OnActionExecuted(ActionExecutedContext filterContext) => throw new NotImplementedException();
    }
}
