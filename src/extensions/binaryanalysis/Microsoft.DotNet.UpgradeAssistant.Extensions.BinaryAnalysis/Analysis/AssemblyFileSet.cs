// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal static class AssemblyFileSet
{
    public static IReadOnlyCollection<string> Create(IReadOnlyList<string> inputPaths)
    {
        var filePaths = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var inputPath in inputPaths)
        {
            if (File.Exists(inputPath))
            {
                filePaths.Add(inputPath);
            }
            else if (Directory.Exists(inputPath))
            {
                var files = Directory.GetFiles(inputPath, "*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    if (string.Equals(Path.GetExtension(file), ".exe", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(Path.GetExtension(file), ".dll", StringComparison.OrdinalIgnoreCase))
                    {
                        filePaths.Add(file);
                    }
                }
            }
            else
            {
                Console.Error.WriteLine($"error: can't find file or directory '{inputPath}'");
                Environment.Exit(1);
            }
        }

        return filePaths;
    }
}
