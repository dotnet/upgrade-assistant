using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace TestProject.TestClasses
{
    public class AM0010
    {
        [AllowHtml]
        [Required]
        public string Property1 { get; set; }

        [AllowHtmlAttribute]
        [MyNamespace.AllowHtml]
        public int Property2 { get; }

        [Foo.AllowHtml, Required]
        public double Property3 { set { } }
    }
}
