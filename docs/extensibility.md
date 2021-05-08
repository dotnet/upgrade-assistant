## Extensibility

The Upgrade Assistant has an extension system that make it easy for users to customize many of the upgrade steps (or add new upgrade steps) without having to rebuild the tool. Extensibility points include:

1. Source updates (via Roslyn analyzers and code fix providers)
2. NuGet package updates (by explicitly mapping certain packages to their replacements)
3. Custom template files (files that should be added to upgraded projects)
4. Config file updates (components that update the project based on the contents of app.config and web.config)
5. Custom upgrade steps (allowing complete freedom to add whatever behaviors are necessary to the upgrade process)

To create an Upgrade Assistant extension, you will need to start with a manifest file called "ExtensionManifest.json". The manifest file contains pointers to the paths (relative to the manifest file) where the different extension items can be found. The extension manifest is required, but all of its elements are optional and it is only necessary to include the ones that are useful for the extension the manifest is describing. An outline of possible extension manifest elements is:

```json
{
  "ExtensionName": "My extension",

  "PackageUpdater": {
    "PackageMapPath": "PackageMaps"
  },
  "TemplateInserter": {
    "TemplatePath": "Templates"
  },

  "ExtensionServiceProviders": [
    "MyExtensionLibrary.dll"
  ]
}
```

To use an extension at runtime, either use the `--extension` argument to point to the extension's manifest file (or directory where the manifest file is located) or set the environment variable "UpgradeAssistantExtensionPaths" to a semicolon-delimited list of paths to probe for extensions.

### Custom upgrade steps, source updaters, and config updaters

The ExtensionServiceProviders element of the extension manifest contains an array of assemblies that the Upgrade Assistant should look in for implementations of `Microsoft.DotNet.UpgradeAssistant.Extensions.IExtensionServiceProvider`. At runtime, Upgrade Assistant will load any assemblies listed in the ExtensionServiceProviders array (paths are relative to the extension manifest's location) and instantiate an public implementations of `IExtensionServiceProvider` found in those assemblies. The `IExtensionServiceProvider` instances will then be used to register services in Upgrade Assistant's dependency injection container. Common services that an extension might register include:

1. Custom upgrade steps (inheriting from `UpgradeStep`). Any upgrade steps registered by an `IExtensionServiceProvider` in an assembly listed in the ExtensionServiceProviders array will be included in the upgrade steps the Upgrade Assistant executes. In this way, extenders can add their own custom steps to the upgrade pipeline.
2. Roslyn analyzers and code fix providers. Upgrade Assistant's source updater step looks in the dependency injection container for any analyzers with associated code fix providers and will include them in the sub-steps to the source updater step. So, by registering their own Roslyn analyzers and code fix providers, extenders can customize the source update steps used by Upgrade Assistant.
3. `IConfigUpdater` implementations. Upgrade Assistant's config updater step uses any registered `IConfigUpdater` services to make project updates based on config files (app.config, web.config). Therefore, by registering their own `IConfigUpdater` implementations, extenders can customize how config-related upgrades are made by Upgrade Assistant.
4. `IPackageReferencesAnalyzer` implementations. For more information on how the package updater step works, see the next section of this document. If providing custom package mapping configuration is insufficient, however, extenders can register their own implementations of `IPackageReferenceAnalzyer` to more completely customize how NuGet package references are updated.
5. Any other services that might be needed by Upgrade Assistant steps (either the default steps or those added by extensions). Extenders can register services that their own upgrade steps will need or services that will be used by other upgrade steps and Upgrade Assistant will make sure any services registered in an `IExtensionServiceProvider` implementation will be made available at runtime.

### Registering Custom Configuration

An extension can register configuration options that can then be added to by other extensions. This can be done similar to the following:

```csharp
using Microsoft.DotNet.UpgradeAssistant.Extensions;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public class TestExtension : IExtensionServiceProvider
    {
        public void AddServices(IExtensionServiceCollection services)
        {
            services.AddExtensionOption<SomeOptions>("sectionName");
        }
    }

    public class SomeOptions
    {
    }
}
```

This can then be used in a couple of ways:

```csharp
using Microsoft.DotNet.UpgradeAssistant.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public class SomeOptions
    {
    }

    public class TestService
    {
        public TestService(
          IOptions<SomeOptions> options, 
          IOptions<OptionCollection<SomeOptions>> collection,
          IOptions<OptionCollection<FileOption<SomeOptions>>> withFiles)
        {
        }
    }
}
```

Where each of these are various forms of the same options:

- `IOptions<SomeOptions>` is a single instance where each source is layered on top of the other
- `IOptions<OptionCollection<SomeOptions>>` is a collection of all of the options from each of the extensions
- `IOptions<OptionCollection<FileOption<SomeOption>>>` is a collection of all of the options for each extension with an accompanying `IFileProvider` that allows access to files within that extension. This is useful of the options map to files within the extension.

### Mapping custom configuration to files

Often, an extension will allow for a list of files to be used to map to other types (see the PackageUpdater or TemplateUpdater steps for full examples):

```csharp
using System.Collections.Generic;
using Microsoft.DotNet.UpgradeAssistant.Extensions;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public class TestExtension : IExtensionServiceProvider
    {
        public void AddServices(IExtensionServiceCollection services)
        {
            services.AddExtensionOption<SomeOptions>("sectionName")
                .MapFiles<SomeOtherOption>(o => o.Files, isArray: true);
        }
    }

    public class SomeOptions
    {
        public IEnumerable<string> Files { get; set; }
    }

    public class SomeOtherOption
    {
    }
}
```

As in the above section, this mapped file may be retrieved through various `IOptions<>` access patterns:

- `IOptions<OptionCollection<SomeOtherOption>>` is a collection of all of the options from each of the extensions
- `IOptions<OptionCollection<FileOption<SomeOtherOption>>>` is a collection of all of the options for each extension with an accompanying `IFileProvider` that allows access to files within that extension. This is useful of the options map to files within the extension.

### Custom NuGet package mapping configuration

The Package updater step of the Upgrade Assistant attempts to update NuGet package references to versions that will work with .NET 5.0. There are a few rules the upgrade step uses to make those updates:

  1. It removes packages that other referenced packages depend on (transitive dependencies). [Try-convert](https://github.com/dotnet/try-convert) moves all packages from packages.config to PackageReference references, but PackageReference-style references only need to include top level packages. The NuGet package updater step removes package references that appear to be transitive so that only top-level dependencies are included in the csproj.
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
