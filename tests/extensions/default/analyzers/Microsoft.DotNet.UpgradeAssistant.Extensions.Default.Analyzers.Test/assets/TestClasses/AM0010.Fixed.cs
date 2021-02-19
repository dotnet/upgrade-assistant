using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace TestProject.TestClasses
{
    public class AM0010
    {
        [Required]
        public string Property1 { get; set; }

        [MyNamespace.AllowHtml]
        public int Property2 { get; }

        [Required]
        public double Property3 { set { } }
    }
}
