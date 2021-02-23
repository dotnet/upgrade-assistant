using System.Web;
using AspNetUpgrade;
using System.Diagnostics;

namespace TestProject.TestClasses
{
    public class AM0006
    {
        public bool IsDebuggingEnabled => Debugger.IsAttached;

        public bool Method1()
        {
            var b = new HttpContext();
            var x = Debugger.IsAttached;
            if (this.IsDebuggingEnabled)
            {
                return Debugger.IsAttached ? true : x;
            }

            return Debugger.IsAttached;

            HttpContext GetCurrentHttpContext() => HttpContext.Current;
        }
    }
}
