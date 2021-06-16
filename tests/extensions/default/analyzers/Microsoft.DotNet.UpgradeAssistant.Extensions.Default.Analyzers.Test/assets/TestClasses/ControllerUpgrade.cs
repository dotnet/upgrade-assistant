using System.Web.Http;
using System.Web.Mvc;
using System.Collections.Generic;

namespace TestProject.TestClasses
{
    [ApiController]
    public partial class ValuesController : ApiController
    {
        // GET api/values
        public IEnumerable<string> GetValues()
        {
            return new[] { "value1", "value2" };
        }
    }

    public partial class ValuesController
    {
        // GET api/morevalues
        public IEnumerable<string> GetMoreValues()
        {
            return new[] { "value3", "value4" };
        }
    }

    public class MoviesController : System.Web.Http.ApiController
    {
        // GET api/values
        public IEnumerable<string> GetValues()
        {
            return new[] { "Star Wars", "Iron Man", "Star Trek" };
        }
    }

    public class NotAWebController : Foo.ApiController
    {

    }

    public partial class Controller2
    {
        Bar.Controller Controller;
    }

    public partial class Controller2 : System.Web.Mvc.Controller
    {
        public Controller DoSomething(ApiController a)
        {
            var x = new List<System.Web.Mvc.Controller>();
            return new Controller2();
        }
    }

    public class NotAController : Foo.Controller
    {

    }
}

namespace Foo
{
    public class ApiController
    {

    }

    public class Controller
    {

    }
}
