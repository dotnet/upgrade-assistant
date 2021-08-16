# Loose binary identification with Upgrade Assistant

It is not uncommon in large code bases to have a collection of dependencies that have been persisted as only the `.dll` files with no known provenance. These files are considered "loose binaries". This makes it challenging to identify what a binary is while upgrading where an updated version is often needed to support .NET Standard/Core/5. In order to solve this, this document proposes integration with a data source that can help match these loose assemblies to known NuGet packages.

The algorithm used to match the binaries makes use of fingerprints that may be useful in identifying what it may be:

- Assembly name
- Public key token
- File contents (and hashes thereof)
- and others

Work has already been done to generate a database of known packages along these and other identity dimensions. This data can then be used to generate potential indexes that could be used from within Upgrade Assistant to identify along these potential dimensions, both for notification and even to update a project file to reference the NuGet package rather than the loose assembly.

## User Experience

The main scenario this will focus on is one where a project has downloaded an assembly and added it to its repo and referenced it from various projects. The end result of this work should be able to:

- Identify what package the dependency came from
- Update a project file to reference the NuGet package rather than a binary reference

As a user, in order to run the extension, they will need to add the extension to the project:

1. Set an environment variable for `UA_FEATURES=EXTENSION_MANAGEMENT`. Extension management is currently under preview, but is required to test this feature.
2. Install the extension for identifying loose assemblies:
   ```
   upgrade-assistant extensions add Microsoft.DotNet.UpgradeAssistant.LooseAssembly`
   ```
3. Install the extension for the data to match against NuGet.org:
   ```
   upgrade-assistant extensions add Microsoft.DotNet.UpgradeAssistant.LooseAssembly.NuGet
   ```

After these have been installed, users on a new machine must restore the packages:

1. Set an environment variable for `UA_FEATURES=EXTENSION_MANAGEMENT`. Extension management is currently under preview, but is required to test this feature.
2. Restore the extensions installed
   ```
   upgrade-assistant extensions restore
   ```

In order to update to the latest versions, the extensions may be updated:

1. Set an environment variable for `UA_FEATURES=EXTENSION_MANAGEMENT`. Extension management is currently under preview, but is required to test this feature.
2. Update the extensions installed
   ```
   upgrade-assistant extensions update
   ```

## Challenges

There are a number of challenges with this, as it is completely based on heuristics in order to match package identities. Potential limitations:

- If an assembly was never published in a package on NuGet.org, it will never show up in the available indexes
- Private builds of projects may not match external builds
- Currently only planning on supporting packages from NuGet.org so loose assemblies from a company's internal teams will not be found. May be included in future work.
- Some loose assemblies may be "ride-along" in packages that they do not actually belong to (ie Newtonsoft.Json is included in a number of packages)
- Early NuGet days had evolving best practice
- NetStandard and other modern patterns are represented as bifurcations in package publish (ex. Foo and Foo.NetStandard packages)

## Planned Work

Below are the main work items that will be explored broken into two sprints. There is currently a pre-analysis migration effort being worked on, and this work may be updated to ensure that the information flows as expected into the reports generated there as those features become available.

### Sprint 1
- As a user, I can consume an extension that identifies loose assemblies (#482)
- As a user, I can identify what loose assemblies I have (#483)
- As a user, I can replace my loose assemblies with identified packages (#484)
- As a user, I know which loose assemblies do not have any available matches (#485)

### Sprint 2
- As a user, I can update my loose assembly data index independently of the extension (#486)
- As a user, I can manage data steps need at a global level (#487)
- As a user, I can receive recommendations from multiple indexes for loose assembly identification (#488)

## Out of scope/Future Work

Some work that are currently not in scope:

- Custom feeds (such as AzDO, Artifactory, file system, etc). NuGet.org will be the focus of the initial spike
- Providing tooling to generate custom indexes
- Identifying any security concerns with switching to NuGet packages. We may want to explore identifying if a package has a security concern, but it's still probably the same binary.
- Hosting the lookup in any sort of service. This design is to integrate the analysis in as simply as possible without external dependencies.
