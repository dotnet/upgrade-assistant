using System.Web.Http;
using System.Web.Mvc;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace TestProject.TestClasses
{
    public partial class ValuesController : ControllerBase
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

    public class MoviesController : ControllerBase
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
        Controller Controller;
    }

    public partial class Controller2 : Controller
    {
        public Controller DoSomething(ControllerBase a)
        {
            var x = new List<Controller>();
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
