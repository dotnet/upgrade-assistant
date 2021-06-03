# Upgrade Assistant samples

This directory contains sample projects demonstrating how to create Upgrade Assistant add-ons using its [extensibility model](../docs/extensibility.md).

## Using the samples

To test out a sample, build the sample's solution (if the sample includes a project file) and then start Upgrade Assistant with the sample extension registered. This can be done in two ways:

1. Use the -e command line parameter when launching Upgrade Assistant and give it the path to either the extension's manifest file (ExtensionManifest.json) or the path where that file is located.
1. Set the environment variable `UpgradeAssistantExtensionPathsSettingName` to the extension manifest's path or directory. This environment variable can reference multiple extensions delimited by semicolons.

## Samples

| Sample | Features demonstrated |
| ------ | --------------------- |
| [FindReplaceStepSample](./FindReplaceStepSample) | Demonstrates how to create an extension with a custom upgrade step that finds and replaces text snippets configured in extension options. |
| [PackageMapSample](./PackageMapSample) | Demonstrates how to create an extension with configuration specifying NuGet package dependency replacements. This sample is made entirely of config files, so there's no project to build. |
| [SourceUpdaterSample](./SourceUpdaterSample) | Demonstrates how to add custom source update behaviors using Roslyn analyzers and code fix providers. |
| [UpgradeStepSample](./UpgradeStepSample) | Demonstrates how to create custom upgrade steps by making a sample upgrade step that ensures upgrade project files include a NuGet `<Authors>` property. |
