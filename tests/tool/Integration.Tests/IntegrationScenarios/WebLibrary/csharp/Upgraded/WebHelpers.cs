using System.Web;
using System.Web.Mvc;
using WebLibrary;

namespace Helper
{
    public static class WebHelpers
    {
        public static string GetClientAddress() =>
            Newtonsoft.Json.JsonConvert.SerializeObject(new { Verb = HttpVerbs.Get, Address = HttpContextHelper.Current.Request.UserHostAddress });
    }
}
