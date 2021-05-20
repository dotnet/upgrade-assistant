// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers
{
    public class WebTypeReplacements : AdditionalText
    {
        private static readonly Dictionary<string, string> Replacements = new Dictionary<string, string>()
        {
            { "System.Web.Http.ApiController", "Microsoft.AspNetCore.Mvc.ControllerBase" },
            { "System.Web.Mvc.Controller", "Microsoft.AspNetCore.Mvc.Controller" },
            { "System.Web.Mvc.ResultExecutingContext", "Microsoft.AspNetCore.Mvc.Filters.ResultExecutingContext" },
            { "System.Web.Mvc.ResultExecutedContext", "Microsoft.AspNetCore.Mvc.Filters.ResultExecutedContext" },
            { "System.Web.Mvc.IResultFilter", "Microsoft.AspNetCore.Mvc.Filters.IResultFilter" },
            { "System.Web.Mvc.ActionFilterAttribute", "Microsoft.AspNetCore.Mvc.Filters.ActionFilterAttribute" },
            { "System.Web.Mvc.ActionExecutingContext", "Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext" },
            { "System.Web.Mvc.ActionExecutedContext", "Microsoft.AspNetCore.Mvc.Filters.ActionExecutedContext" },
            { "System.Web.Mvc.IActionFilter", "Microsoft.AspNetCore.Mvc.Filters.IActionFilter" },
            { "System.Web.WebPages.HelperResult", "Microsoft.AspNetCore.Mvc.Razor.HelperResult" },
            { "System.Web.HtmlString", "Microsoft.AspNetCore.Html.HtmlString" },
            { "System.Web.IHtmlString", "Microsoft.AspNetCore.Html.HtmlString" },
            { "System.Web.Mvc.MvcHtmlString", "Microsoft.AspNetCore.Html.HtmlString" },
            { "System.Web.Mvc.ActionResult", "Microsoft.AspNetCore.Mvc.ActionResult" },
            { "System.Web.Mvc.ContentResult", "Microsoft.AspNetCore.Mvc.ContentResult" },
            { "System.Web.Mvc.FileResult", "Microsoft.AspNetCore.Mvc.FileResult" },
            { "System.Web.Mvc.HttpNotFoundResult", "Microsoft.AspNetCore.Mvc.NotFoundResult" },
            { "System.Web.Mvc.HttpStatusCodeResult", "Microsoft.AspNetCore.Mvc.StatusCodeResult" },
            { "System.Web.Mvc.HttpUnauthorizedResult", "Microsoft.AspNetCore.Mvc.UnauthorizedResult" },
            { "System.Web.Mvc.RedirectResult", "Microsoft.AspNetCore.Mvc.RedirectResult" },
            { "System.Web.Mvc.PartialViewResult", "Microsoft.AspNetCore.Mvc.PartialViewResult" },
            { "System.Web.Mvc.ViewResult", "Microsoft.AspNetCore.Mvc.ViewResult" },
        };

        public override string Path => "WebTypeReplacements.typemap";

        public override SourceText? GetText(CancellationToken cancellationToken = default) =>
            SourceText.From(string.Join("\n", Replacements.Select(kvp => $"{kvp.Key}\t{kvp.Value}")));
    }
}
