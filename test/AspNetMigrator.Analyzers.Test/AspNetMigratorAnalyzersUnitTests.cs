using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using VerifyCS = AspNetMigrator.Analyzers.Test.CSharpCodeFixVerifier<
    AspNetMigrator.Analyzers.UsingSystemWebAnalyzer,
    AspNetMigrator.Analyzers.UsingSystemWebFixer>;

namespace AspNetMigrator.Analyzers.Test
{
    [TestClass]
    public class AspNetMigratorAnalyzersUnitTest
    {
        //No diagnostics expected to show up
        [TestMethod]
        public async Task NegativeTest()
        {
            var test = @"";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public async Task PositiveTest()
        {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    {|#0:using System.Web;|}
    {|#1:using System.Web.Mvc;|}
    using System.Diagnostics;
    {|#2:using Owin;|}

    namespace  System.Web.Mvc { public class A { } }
    namespace  Owin { public class A { } }

    namespace ConsoleApplication1
    {
        class TypeName
        {   
        }
    }";

            var fixtest = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace  System.Web.Mvc { public class A { } }
    namespace  Owin { public class A { } }

    namespace ConsoleApplication1
    {
        class TypeName
        {   
        }
    }";

            var expected = new DiagnosticResult[] {
                VerifyCS.Diagnostic("AM0001").WithLocation(0).WithArguments("System.Web"),
                VerifyCS.Diagnostic("AM0001").WithLocation(1).WithArguments("System.Web.Mvc"),
                VerifyCS.Diagnostic("AM0001").WithLocation(2).WithArguments("Owin")
            };
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }
    }
}
