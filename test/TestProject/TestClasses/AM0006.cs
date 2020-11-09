using System.Web;
using AspNetMigration;

namespace TestProject.TestClasses
{
    public class AM0006
    {
        public bool IsDebuggingEnabled => HttpContext.Current.IsDebuggingEnabled;

        public bool Method1()
        {
            var b = new HttpContext();
            var x = b.IsDebuggingEnabled;
            if (this.IsDebuggingEnabled)
            {
                return HttpContextHelper.Current.IsDebuggingEnabled ? true : x;
            }

            return GetCurrentHttpContext().IsDebuggingEnabled;

            HttpContext GetCurrentHttpContext() => HttpContext.Current;
        }
    }
}
