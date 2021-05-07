using System.Web.Http;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace TestProject.TestClasses
{
    public class ValuesController : Controller
    {
        // GET api/values
        public IEnumerable<string> GetValues()
        {
            return new[] { "value1", "value2" };
        }
    }

    public class MoviesController : Controller
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
}

namespace Foo
{
    public class ApiController
    {

    }
}
