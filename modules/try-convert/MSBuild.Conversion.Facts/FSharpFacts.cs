namespace MSBuild.Conversion.Facts
{
    /// <summary>
    /// A bunch of known values regarding legacy F# projects.
    /// </summary>
    public static class FSharpFacts
    {
        public const string FSharpTargetsPathVariableName = @"$(FSharpTargetsPath)";
        public const string FSharpTargetsPath = @"$(MSBuildExtensionsPath32)\Microsoft\VisualStudio\v$(VisualStudioVersion)\FSharp\Microsoft.FSharp.Targets";
    }
}
