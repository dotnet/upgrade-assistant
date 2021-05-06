# Roadmap
This tool is just one piece of a bigger journey we’re on to streamline the journey to upgrade your apps from .NET Framework to .NET 5 and beyond.
This list is not an exhaustive list of all features and support coming – but rather the ones most requested and/or prioritized to keep your upgrades on track. If there’s anything missing from this list that you expect to see or you’re having difficulty with in your upgrade, please let us know via an [issues](https://github.com/dotnet/upgrade-assistant/issues).

The priority or state for an issue can change at any time.

**1. Pre-Upgrade analysis**

Take a look at the design doc for [design details](https://github.com/dotnet/upgrade-assistant/blob/main/docs/design/Pre-UpgradeAnalysis.md) and phases of implementation.

- **Committed**
  - [Loose assemblies analysis](https://github.com/dotnet/upgrade-assistant/blob/main/docs/design/Pre-UpgradeAnalysis.md#loose-assembly-analysis)
  -	More visibility into [NuGet dependencies](https://github.com/dotnet/upgrade-assistant/blob/main/docs/design/Pre-UpgradeAnalysis.md#nuget-package-dependecy-analysis) (e.g. newer versions, do they support .NET 5+?), transitive dependencies, and [inter-project dependencies](https://github.com/dotnet/upgrade-assistant/blob/main/docs/design/Pre-UpgradeAnalysis.md#inter-project-dependencies)
- **On Deck**
  -	[Up front checks for technologies that might not be supported in .NET 5+](https://github.com/dotnet/upgrade-assistant/blob/main/docs/design/Pre-UpgradeAnalysis.md#surface-unsupported-api-categories) or could make the upgrade effort large in cost (links to docs, options available, best practices, etc.).

**2. Upgrade**

- **Committed**
  - [Choose between LTS (long-term servicing), Current (.NET 5) or Preview (.NET 6 Preview) for the target .NET version](https://github.com/dotnet/upgrade-assistant/issues/41)
  - [Add more ASP.NET app analyzers for code fixes](https://github.com/dotnet/upgrade-assistant/issues/55)
  - Update non-C# source:
    - [VB](https://github.com/dotnet/upgrade-assistant/issues/270)
    - [Cshtml](https://github.com/dotnet/upgrade-assistant/issues/57)
  - Ability to update NuGet dependencies update across a solution instead of project at a time to avoid conflicting cross-dependencies

- **On Deck**
  - [GC Config Settings](https://github.com/dotnet/upgrade-assistant/issues/399)
  - Post-migration details with all the changes made and brings attention to any potentially breaking changes
  - Multitargeting support for specific and multiple target frameworks
  - Web Forms support
  - Even more ideas listed in the repo [issues](https://github.com/dotnet/upgrade-assistant/issues)

**3. Tooling support**

Example areas:
- Tool maintains the state of the upgrade
- UI on top of the CLI experience
