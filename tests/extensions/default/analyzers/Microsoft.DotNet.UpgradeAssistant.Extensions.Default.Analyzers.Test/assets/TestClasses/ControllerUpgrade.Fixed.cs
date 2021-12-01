using System.Web.Http;
using System.Web.Mvc;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace TestProject.TestClasses
{
    [ApiController]
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
        Bar.Controller Controller;
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
        public string Controller { get; set; }

        public void SomeMethod()
        {
            var anonymousType = new { Controller = "A string" };
            if (anonymousType is null)
            {
                var Controller = anonymousType;
                var ControllerBase = new Bar.ApiController { Controller = Controller };
            }

            var x = new Controller();
            var y = Controller.Foo();       // Property access
            var y1 = ControllerBase.Foo();   // Static class access
            var z = new Controller[10];
            ControllerBase x = null;
            var z1 = new Foo.ApiController().Controller;
        }

        public Controller ControllerProperty { get; set; }
        private ControllerBase ControllerField;
    }
}

namespace Foo
{
    public class ApiController
    {
        object Controller
        {
            get;

            set
            {
                var ApiController = string.Empty;
                var x = ApiController.Substring(0, 0);
            }
        }
    }

    public class Controller
    {

    }
}
