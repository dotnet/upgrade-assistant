using System.Web.Http;
using System.Collections.Generic;

namespace TestProject.TestClasses
{
    public class ValuesController : ApiController
    {
        // GET api/values
        public IEnumerable<string> GetValues()
        {
            return new[] { "value1", "value2" };
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
}

namespace Foo
{
    public class ApiController
    {

    }
}
