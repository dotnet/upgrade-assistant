using System;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.UpgradeAssistant.Portability
{
    public class PortabilityOptions
    {
        [Required]
        public Uri ServiceEndpoint { get; set; } = null!;
    }
}
