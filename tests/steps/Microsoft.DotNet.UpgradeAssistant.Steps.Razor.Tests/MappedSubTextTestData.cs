﻿namespace Razor
{
#line hidden
#line 1 "test.cshtml"
    using System;

#line default
#line hidden
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA1", @"38e4e7b9907aaf0dbd917f25e1c2da15895aa64f", @"/test.cshtml")]
    public class Template
    {
#line hidden
#pragma warning disable 0169
        private string __tagHelperStringValueBuffer;
#pragma warning restore 0169
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperExecutionContext __tagHelperExecutionContext;
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner __tagHelperRunner = new global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner();
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager __backed__tagHelperScopeManager = null;
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager __tagHelperScopeManager
        {
            get
            {
                if (__backed__tagHelperScopeManager == null)
                {
                    __backed__tagHelperScopeManager = new global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager(StartTagHelperWritingScope, EndTagHelperWritingScope);
                }
                return __backed__tagHelperScopeManager;
            }
        }
        private global::PTagHelper __PTagHelper;
#pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
#line 3 "test.cshtml"

            var foo = "Hello World!";

#line default
#line hidden
            WriteLiteral("\n");
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("p", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "38e4e7b9907aaf0dbd917f25e1c2da15895aa64f2096", async () => {
                WriteLiteral("Content");
            }
            );
            __PTagHelper = CreateTagHelper<global::PTagHelper>();
            __tagHelperExecutionContext.Add(__PTagHelper);
#line 7 "test.cshtml"
            __PTagHelper.FooProp = 123;

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("foo", __PTagHelper.FooProp, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
            BeginWriteTagHelperAttribute();
#line 7 "test.cshtml"
            WriteLiteral(foo);

#line default
#line hidden
            __tagHelperStringValueBuffer = EndWriteTagHelperAttribute();
            __PTagHelper.BarProp = __tagHelperStringValueBuffer;
            __tagHelperExecutionContext.AddTagHelperAttribute("bar", __PTagHelper.BarProp, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.SingleQuotes);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                await __tagHelperExecutionContext.SetOutputContentAsync();
            }
            Write(__tagHelperExecutionContext.Output);
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
        }
#pragma warning restore 1998
    }
}
#pragma warning restore 1591
