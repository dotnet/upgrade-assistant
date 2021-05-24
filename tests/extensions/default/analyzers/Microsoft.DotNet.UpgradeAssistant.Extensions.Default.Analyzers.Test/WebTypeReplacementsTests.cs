// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Test
{
    public static class WebTypeReplacementsTests
    {
        private static readonly Dictionary<string, string> ExpectedReplacements = new()
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

        [Fact]
        public static void WebTypeReplacementContentTests()
        {
            // Arrange
            var replacementsText = new WebTypeReplacements();

            // Act
            var path = replacementsText.Path;
            var text = replacementsText.GetText()?.ToString() ?? string.Empty;
            var replacements = text.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Split('\t'))
                .ToDictionary(kvp => kvp[0], kvp => kvp[1]);

            // Assert
            Assert.Collection(replacements, ExpectedReplacements.Select<KeyValuePair<string, string>, Action<KeyValuePair<string, string>>>(expected => actual =>
            {
                Assert.Equal(expected.Key, actual.Key);
                Assert.Equal(expected.Value, actual.Value);
            }).ToArray());
        }
    }
}
