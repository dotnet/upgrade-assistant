﻿using System;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TestProject.TestClasses
{
    public class FilterUpgrade : IResultFilter
    {
        public void OnResultExecuted(ResultExecutedContext filterContext)
        {
            throw new NotImplementedException();
        }

        public void OnResultExecuting(ResultExecutingContext filterContext)
        {
            Foo.ResultExecutingContext x = (Foo.ResultExecutingContext)new TestProject.MyNamespace.ResultExecutingContext();
            throw new NotImplementedException();
        }
    }

    public class UA0004B : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            Microsoft.AspNetCore.Mvc.Filters.ActionExecutedContext a = null;
            var x = this as IActionFilter;
            var y = (IActionFilter)this;
        }

        public override void OnActionExecuted(ActionExecutedContext filterContext) => throw new NotImplementedException();
    }
}
