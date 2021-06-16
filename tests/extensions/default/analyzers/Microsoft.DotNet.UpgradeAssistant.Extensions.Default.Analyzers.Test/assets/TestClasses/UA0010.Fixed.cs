using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace TestProject.TestClasses
{
    public class UA0010
    {
        [Required]
        public string Property1 { get; set; }

        [MyNamespace.AllowHtml]
        public int Property2 { get; }

        [Foo.AllowHtml, Required]
        public double Property3 { set { } }

        [Required]
        public double Property3 { set { } }
    }
}
