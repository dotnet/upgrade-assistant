using System;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CSharp.Analyzers
{
    public class IdentifierMapping
    {
        public string OldFullName { get; }

        public string NewFullName { get; }

        public string SimpleName { get; }

        public IdentifierMapping(string oldFullname, string newFullName)
        {
            if (string.IsNullOrEmpty(oldFullname))
            {
                throw new ArgumentException($"'{nameof(oldFullname)}' cannot be null or empty", nameof(oldFullname));
            }

            if (string.IsNullOrEmpty(newFullName))
            {
                throw new ArgumentException($"'{nameof(newFullName)}' cannot be null or empty", nameof(newFullName));
            }

            OldFullName = oldFullname;
            NewFullName = newFullName;

            var finalDotIndex = OldFullName.LastIndexOf('.');
            SimpleName = finalDotIndex < 0 ? OldFullName : OldFullName.Substring(finalDotIndex + 1);
        }
    }
}
