﻿using System;
using Microsoft.AspNetCore.Mvc.Razor;

namespace TestProject.TestClasses
{
    public class HelperResultUpgrade
    {
        public HelperResult Method1(HelperResult r)
        {
            Func<string, HelperResult> x = a => new HelperResult(writer => writer.Write(a));

            return (Foo.HelperResult)r;
        }
    }
}
