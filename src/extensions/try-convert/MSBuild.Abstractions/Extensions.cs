using System;

namespace MSBuild.Abstractions
{
    public static class Extensions
    {
        public static bool ContainsIgnoreCase(this string target, string substring)
        {
            return target.IndexOf(substring, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
