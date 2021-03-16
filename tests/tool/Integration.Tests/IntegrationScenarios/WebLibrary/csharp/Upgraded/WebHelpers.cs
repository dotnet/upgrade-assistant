
namespace Helper
{
    public static class WebHelpers
    {
        public static string GetClientAddress() =>
            Newtonsoft.Json.JsonConvert.SerializeObject(new { Verb = HttpVerbs.Get, Address = HttpContext.Current.Request.UserHostAddress });
    }
}
