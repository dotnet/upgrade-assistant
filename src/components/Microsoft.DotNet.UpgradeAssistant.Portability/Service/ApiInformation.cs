using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;

namespace Microsoft.DotNet.UpgradeAssistant.Portability.Service
{
    public class ApiInformation
    {
        public ApiDefinition Definition { get; set; } = null!;

        public IEnumerable<FrameworkName> Supported { get; set; } = Enumerable.Empty<FrameworkName>();

        public string RecommendedChanges { get; set; } = string.Empty;

        public string Component { get; set; } = string.Empty;

        public override int GetHashCode() => Definition?.GetHashCode() ?? 0;

        /// <summary>
        /// A simple check to see if the API is supported. This should be updated at some point to handle the target the user actually cares about.
        /// </summary>
        public bool IsSupported()
        {
            foreach (var supported in Supported)
            {
                if (supported.Identifier.Contains("Standard", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (supported.Identifier.Contains("Core", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
