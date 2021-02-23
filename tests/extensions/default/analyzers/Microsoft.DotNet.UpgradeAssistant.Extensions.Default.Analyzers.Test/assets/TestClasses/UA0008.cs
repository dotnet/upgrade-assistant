namespace TestProject.TestClasses
{
    public class UA0008 : System.Web.Mvc.UrlHelper
    {
        public UrlHelper Method1(this System.Web.Mvc.UrlHelper h)
        {
            Foo.UrlHelper x = h;

            h.ExtenstionMethod(new TestProject.MyNamespace.UrlHelper(), new UrlHelper());

            System.Web.Mvc.UrlHelper.GenerateUrl(null, null, null, null, null, null, false);

            return h;
        }
    }
}
