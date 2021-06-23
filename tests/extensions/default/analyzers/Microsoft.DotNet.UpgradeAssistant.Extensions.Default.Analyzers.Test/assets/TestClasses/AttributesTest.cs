// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Test.assets.TestClasses
{
    [AllowHtml] //
    public class Class1
    {
    }

    [System.AllowHtml] //
    [OutputCacheAttribute]
    public class Class2
    {
    }

    [Web.Mvc.ValidateInput,System.Web.Mvc.AuthorizeAttribute]
    public class Class3
    {
    }

    [Microsoft.AspNetCore.Mvc.Filters.ActionFilter]
    [System.Web.Mvc.Authorize]
    [ValidateInputAttribute]
    public class Class4
    {
        [Bind]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public void Method1([BindAttribute] string s)
        {

        }
    }
}
