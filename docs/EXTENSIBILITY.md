## Extensibility

The Upgrade Assistant has an extension system that make it easy for users to customize many of the upgrade steps without having to rebuild the tool. An Upgrade Assistant extension can extend the following upgrade steps (described in more detail below):

1. Source updates (via Roslyn analyzers and code fix providers)
2. NuGet package updates (by explicitly mapping certain packages to their replacements)
3. Custom template files (files that should be added to upgraded projects)
4. Config file updates (components that update the project based on the contents of app.config and web.config)

To create an Upgrade Assistant extension, you will need to start with a manifest file called "ExtensionManifest.json". The manifest file contains pointers to the paths (relative to the manifest file) where the different extension items can be found. A typical ExtensionManifest.json looks like this:

```json
{
  "ConfigUpdater": {
    "ConfigUpdaterPath": "ConfigUpdaters"
  },
  "PackageUpdater": {
    "PackageMapPath": "PackageMaps"
  },
  "SourceUpdater": {
    "SourceUpdaterPath": "SourceUpdaters"
  },
  "TemplateInserter": {
    "TemplatePath": "Templates"
  }
}
```

To use an extension at runtime, either use the -e argument to point to the extension's manifest file (or directory where the manifest file is located) or set the environment variable "UpgradeAssistantExtensionPaths" to a semicolon-delimited list of paths to probe for extensions.

### Custom source analyzers and code fix providers

The source updater step of the Upgrade Assistant uses Roslyn analyzers to identify problematic code patterns in users' projects and code fixes to automatically correct them to .NET 5.0-compatible alternatives. Over time, the set of built-in analyzers and code fixes will continue to grow. Users may want to add their own analyzers and code fix providers as well, though, to flag and fix issues in source code specific to libraries and patterns they use in their apps.

To add their own analyzers and code fixes, users should include binaries with their own Roslyn analyzers and code fix providers in the SourceUpdaterPath specified in the extension's manifest. The Upgrade Assistant will automatically pick analyzers and code fix providers up from these extension directories when it starts.

Any type with a `DiagnosticAnalyzerAttribute` attribute is considered an analyzer and any type with an `ExportCodeFixProviderAttribute` attribute is considered a code fix provider.

### Custom NuGet package mapping configuration

The Package updater step of the Upgrade Assistant attempts to update NuGet package references to versions that will work with .NET 5.0. There are a few rules the upgrade step uses to make those updates:

  1. It removes packages that other referenced packages depend on (transitive dependencies). Try-convert moves all packages from packages.config to PackageReference references, but PackageReference-style references only need to include top level packages. The NuGet package updater step removes package references that appear to be transitive so that only top-level dependencies are included in the csproj.
  2. If a referenced NuGet package isn't compatible with the target .NET version but a newer version of the NuGet package is, the package updater step automatically updates the version to the first major version that will work.
  3. The package updater step will replace NuGet references based on specific replacement packages listed in configuration files. For example, there's a config setting that specifically indicates System.Threading.Tasks.Dataflow should replace TPL.Dataflow.

This third type of NuGet package replacement can be customized by users. An extension's PackageMapPath path can contain json files that map old packages to new ones. In each set, there are old (.NET Framework) package references which should be removed and new (.NET 5.0/Standard) package references which should be added. If a project being upgraded by the tool contains references to any of the 'NetFrameworkPackages' references from a set, those references will be removed and all of the 'NetCorePackages' references from that same set will be added. If old (NetFrameworkPackage) package references specify a version, then the references will only be replaced if the referenced version is less than or equal to that version.

By adding package map files, users can supply customized rules about which NuGet package references should be removed from upgraded projects and which NuGet package references (if any) should replace them.

An example package map files looks like this:

```json
[
  {
    "PackageSetName": "TPL.Dataflow",
    "NetFrameworkPackages": [
      {
        "Name": "Microsoft.Tpl.Dataflow",
        "Version": "*"
      }
    ],
    "NetCorePackages": [
      {
        "Name": "System.Threading.Tasks.Dataflow",
        "Version": "4.11.1"
      }
    ]
  }
]
```

### Custom template files

The template inserter step of the Upgrade Assistant adds necessary template files to the project being upgraded. For example, when upgrading a web app, this step will add Program.cs and Startup.cs files to the project to enable ASP.NET Core startup paths. The TemplatePath property of the extension manifest points to a directory that will be probed for TemplateConfig.json files. The files define files that should be added to upgraded projects.

A template config file is a json file containing the following properties:

  1. An array of template items which lists the template files to be inserted. Each of these template items will have a path (relative to the config file) where the template file can be found, and what MSBuild 'type' of item it is (compile, content, etc.). The folder structure of template files will be preserved, so the inserted file's location relative to the project root will be the same as the template file's location relative to the config file. Each template item can also optionally include a list of keywords which are present in the template file. This list is used by the Upgrade Assistant to determine whether the template file (or an acceptable equivalent) is already present in the project. If a file with the same name as the template file is already present in the project, the template inserter step will look to see whether the already present file contains all of the listed keywords. If it does, then the file is left unchanged. If any keywords are missing, then the file is assumed to not be from the template and it will be renamed (with the pattern {filename}.old.{ext}) and the template file will be inserted instead.
  1. A dictionary of replacements. Each item in this dictionary is a key/value pair of tokens that should be replaced in the template file to customize it for the particular project it's being inserted into. MSBuild properties can be used as values for these replacements. For example, many template configurations replace 'WebApplication1' (or some similar string) with '$(RootNamespace)' so that the namespace in template files is replaced with the root namespace of the project being upgraded.
  1. A boolean called UpdateWebAppsOnly that indicates whether these templates only apply to web app scenarios. Many templates that users want inserted are only needed for web apps and not for class libraries or other project types. If this boolean is true, then the template updater step will try to determine the type of project being upgraded and only include the template files if the project is a web app.

If multiple template configurations attempt to insert files with the same path, the template configuration listed last (or in the last added extension) will override earlier ones and its template file will be the one inserted.

An example TemplateConfig.json file looks like this:

```json
{
  "Replacements": {
    "WebApplication1": "$(RootNamespace)"
  },
  "TemplateItems": [
    {
      "Path": "Program.cs",
      "Type": "Compile",
      "Keywords": [
        "Main",
        "Microsoft.AspNetCore.Hosting"
      ]
    },
    {
      "Path": "Startup.cs",
      "Type": "Compile",
      "Keywords": [
        "Configure",
        "ConfigureServices"
      ]
    },
    {
      "Path": "appsettings.json",
      "Type": "Content",
      "Keywords": []
    },
    {
      "Path": "appsettings.Development.json",
      "Type": "Content",
      "Keywords": []
    }
  ],
  "UpdateWebAppsOnly": true
}
```
