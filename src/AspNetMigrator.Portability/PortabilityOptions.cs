using System;
using System.ComponentModel.DataAnnotations;

namespace AspNetMigrator
{
    public class PortabilityOptions
    {
        [Required]
        public Uri ServiceEndpoint { get; set; } = null!;
    }
}
