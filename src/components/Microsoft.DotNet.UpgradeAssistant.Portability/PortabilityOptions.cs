using System;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.DotNet.UpgradeAssistant.Portability
{
    public class PortabilityOptions
    {
        [Required]
        public Uri ServiceEndpoint { get; set; } = null!;
    }
}
