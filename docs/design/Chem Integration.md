# Loose binary identification with Upgrade Assistant

It is not uncommon in large code bases to have a collection of dependencies that have been persisted as only the `.dll` files with no known provenance. These files are considered "loose binaries". This makes it challenging to identify which binary is being used, especially when a different target (ie .NET Standard) is needed. In order to solve this, this document proposes integration with a data source that can help match these loose assemblies to known NuGet packages.

Chem is a tool developed internally to gain insights from the ecosystem of code attached to .NET. As a result of the data that it gathers, it is able to match loose assemblies with their probable origins.  As customers are looking to move to .NET 5, this may pose a challenge as these versions are highly likely to be .NET Framework compilations that may not work as expected on newer platforms. Often, these libraries have NuGet packages now and newer versions that could work on newer platforms.

The algorithm used to match the binaries makes use of fingerprints that may be useful in identifying what it may be:

- Assembly name
- Public key token
- File contents (and hashes thereof)
- and others

Work has already been done to generate a database of known packages along these and other identity dimensions. This data can then be used to generate potential indexes that could be used from within Upgrade Assistant to identify along this potential dimensions, both for notification and even to update a project file to reference the NuGet package rather than the loose assembly.

## User Experience

The main scenario this will focus on is one where a project has downloaded an assembly and added it to its repo and referenced it from various projects. The end result of this work should be able to:

- Identify what package the dependency came from
- Update a project file to reference the NuGet package rather than a binary reference

The expected UX for this will be to implement it as an extension that a user can opt into. As a user, in order to run the Chem integration, they will obtain the extension (ie `UpgradeAssitantChem.zip`) and then run as they normally would:

```
upgrade-assistant some-project.vbproj --extension .\path\to\UpgradeAssistantChem.zip
```

### Decoupling the data
Initially, any data needed will be provided with the extension. This data would be what is used to map the fingerprints to actual packages. They often range around the 100mb-200mb range for useful data, but could be potentially larger if desired.

However, it may be nice to decouple the data from the actual extension itself so they can be updated independently. In order to support this, Upgrade Assistant should provide a way for steps/extensions to provide step-dependent data. This could be something that a step could register it as having, and then Upgrade Assistant could provide commands to view and update. An example of what this might look like is the following:

- `upgrade-assistant manage-data list`: List registered data sources
- `upgrade-assistant manage-data update --name [name]`: Update a registered data source (managed by the step itself)

There are potentially many questions here as to how to handle the data. This is provided solely as a way to demonstrate a potential way to decouple the data required. Initially, the data will be included with the extension, but potentially have optional indexes (or indexes that carry more data and are much larger) will require some sort of way to manage the data.

## Challenges

There are a number of challenges with this, as it is completely based on heuristics in order to match package identities. Potential limitations:

- If an assembly was never published in a package on NuGet.org, it will never show up in the available indexes
- Private builds of projects may not match external builds
- Currently only planning on supporting packages from NuGet.org so loose assemblies from a company's internal teams will not be found. May be included in future work.

## Planned Work

Below are the main work items that will be explored broken into two sprints. There is currently a pre-analysis migration effort being worked on, and this work may be updated to ensure that the information flows as expected into the reports generated there as those features become available.

### Sprint 1
- As a user, I can consume an extension that identifies loose assemblies (#482)
- As a user, I can identify what loose assemblies I have (#483)
- As a user, I can replace my loose assemblies with identified packages (#484)
- As a user, I know which loose assemblies do not have any available matches (#485)

### Sprint 2
- As a user, I can update my chem data index independently of the extension (#486)
- As a user, I can manage data steps need at a global level (#487)
- As a user, I can receive recommendations from chem using multiple indexes (#488)

## Out of scope/Future Work

Some work that are currently not in scope:

- Custom feeds (such as AzDO, Artifactory, file system, etc). NuGet.org will be the focus of the initial spike
- Providing tooling to generate custom indexes
- Identifying any security concerns with switching to NuGet packages. We may want to explore identifying if a package has a security concern, but it's still probably the same binary.
- Hosting the chem lookup in any sort of service. This design is to integrate Chem in as simply as possible without external dependencies.
