using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Microsoft.UpgradeAssistant
{
    public readonly struct ProjectItemMatcher
    {
        private readonly object _match;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectItemMatcher"/> struct that
        /// matches using regex using just the filename of the input.
        /// </summary>
        /// <param name="regex">The regular expression to be used to match.</param>
        public ProjectItemMatcher(Regex regex)
        {
            _match = regex;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectItemMatcher"/> struct that
        /// matches with the given string against the end of the input.
        /// </summary>
        /// <param name="match">The string to be used to match.</param>
        public ProjectItemMatcher(string match)
        {
            _match = match;
        }

        public bool Match(string input)
            => _match switch
            {
                Regex regex => regex.IsMatch(Path.GetFileName(input)),
                string str => str.EndsWith(input, StringComparison.OrdinalIgnoreCase),
                _ => throw new NotImplementedException(),
            };

        public static implicit operator ProjectItemMatcher(Regex regex)
            => new(regex);

        public static implicit operator ProjectItemMatcher(string str)
            => new(str);
    }
}
