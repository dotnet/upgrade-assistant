using System.Collections.Generic;
using System.Web;
using System.Web.Http;
using HybridExample;
using WebApplication1;

namespace WebApplication1.Controllers
{
    public class ValuesController : ApiController
    {
        // GET api/values
        public IEnumerable<string> Get()
        {
            var c = new Class1();
            return new string[] { c.Method(HttpContext.Current) };
        }

        // GET api/values/5
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
        public void Post([FromBody] string value)
        {
        }

        // PUT api/values/5
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/values/5
        public void Delete(int id)
        {
        }
    }
}
