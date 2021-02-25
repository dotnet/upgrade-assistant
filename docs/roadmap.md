# Roadmap

This tool is just one piece of a bigger journey we’re on to streamline the journey to upgrade your apps from .NET Framework to .NET 5 and beyond.

Below is a preview of some of the areas we want to focus on in that journey and also areas we want to invest in for this tool. If there’s anything missing from this proposed list that you expected to see or you’re having difficulty with in your upgrade, please let us know via an [issue]( https://github.com/dotnet/upgrade-assistant/issues).

This list is not meant to be exhaustive of all features and [issues](https://github.com/dotnet/upgrade-assistant/issues) that could be supported, just a high level overview.

**1. Pre-migration analysis**
Example areas:
- Up front checks for technologies that might not be supported in .NET 5+ or could make the upgrade effort large in cost (links to docs, options available, best practices, etc.).
- More visibility into NuGet dependencies (e.g. newer versions, do they support .NET 5+?)
- Recommendations for TFM (target frameworks) to move to

**2. Upgrade**
Example areas:
- Multitargeting support for specific and multiple target frameworks
- Identify areas of an app that might require manual changes
- Add more ASP.NET app analyzers for code fixes 
- Add Windows app analyzers for code fixes
- Update non-C# source like cshtml, xaml, vb, etc.
- Post-migration details with all the changes made and brings attention to any potentially breaking changes
- Even more ideas listed in the repo [issues](https://github.com/dotnet/upgrade-assistant/issues)

**3. Tooling support**
Example areas:
- Tool maintains the state of the upgrade
- UI on top of the CLI experience
