using System;
using System.Net.Http;

namespace SamplePCL
{
    public class Class1
    {
        public void Method1()
        {
            var httpClient = new HttpClient(new HttpClientHandler())
            {
                BaseAddress = new Uri("http://localhost")
            };
        }
    }
}
