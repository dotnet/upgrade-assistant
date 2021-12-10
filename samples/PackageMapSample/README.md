# Upgrade Assistant package map sample

This sample demonstrates how to customize how NuGet package references are upgraded by Upgrade Assistant.

While upgrading NuGet package references, Upgrade Assistant applies a set of rules to identify necessary changes:

1. References that appear to be transitive dependencies are removed.
1. References that don't support the upgraded target framework moniker are updated to versions that do support it (if possible).
1. References to well-known useful packages (like the Windows compatibility pack or the Upgrade Assistant analyzers) are added, if applicable.
1. Certain well-known package references are replaced with updated references that work on .NET 6. This is distinct from step 2 because that step might upgrade a `Newtonsoft.Json` version 6 reference to version 9, whereas this step might replace a reference to `Microsoft.Tpl.Dataflow` with a reference to `System.Threading.Tasks.Dataflow` instead.

Users can customize package reference update behavior by extending that final step. The packages that are replaced are defined in "package map" config files. Upgrade Assistant extensions can include their own package map config files which will be used alongside the built-in ones, as demonstrated [in this sample](./PackageMapSample/SamplePackageMaps/SamplePackageMap.json).
