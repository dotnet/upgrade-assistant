# Extensibility <!-- omit in toc -->

The Upgrade Assistant has an extension system that make it easy for users to customize many of the upgrade steps (or add new upgrade steps) without having to rebuild the tool. There are both code and non-code ways of extending the tool.

- [Create Extension](#create-extension)
- [Custom assemblies](#custom-assemblies)
- [Publishing to NuGet.org](#publishing-to-nugetorg)
- [Extension Service Providers](#extension-service-providers)
  - [Registering Services Configuration](#registering-services-configuration)
  - [Accessing extension files](#accessing-extension-files)
  - [Mapping custom configuration to files](#mapping-custom-configuration-to-files)
- [Steps](#steps)
- [Analyzers/code fixers](#analyzerscode-fixers)
- [Updaters](#updaters)
- [Dependency analyzers](#dependency-analyzers)
- [Templates](#templates)

## Create Extension

To create an Upgrade Assistant extension, you will need to start with a manifest file called `ExtensionManifest.json`. The manifest file contains pointers to the paths (relative to the manifest file) where the different extension items can be found. The extension manifest is required, but all of its elements are optional and it is only necessary to include the ones that are useful for the extension the manifest is describing. An outline of possible extension manifest elements is:

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

After creating your extension, please make sure to build it to generate the `MyExtensionLibrary.dll` file. After building, the `ExtensionManifest.json` will be copied into the same output directory as the `MyExtensionLibrary.dll`. When using the extension, you need to point to the path of this output directory, or the `ExtensionManifest.json` inside this output directory. 

An extension can be available as:

- NuGet package on NuGet.org (see [below](#publishing-to-nugetorg) for details)
- Just the `ExtensionManifest.json`
- A directory containing `ExtensionManifest.json`
- A zip file containing a `ExtensionManifest.json`

To use an extension at runtime, you may either:

- Add an extension to a project via `upgrade-assistant extensions add [name]` if it's available on a NuGet feed
- Use the `--extension` argument on the commandline
- Set the environment variable `UpgradeAssistantExtensionPaths` to a semicolon-delimited list of paths to probe for extensions.

## Custom assemblies

The `ExtensionServiceProviders` element of the extension manifest contains an array of assemblies that the Upgrade Assistant should look in for implementations of `Microsoft.DotNet.UpgradeAssistant.Extensions.IExtensionServiceProvider`. At runtime, Upgrade Assistant will load any assemblies listed in the ExtensionServiceProviders array (paths are relative to the extension manifest's location) and instantiate an public implementations of `IExtensionServiceProvider` found in those assemblies. The `IExtensionServiceProvider` instances will then be used to register services in Upgrade Assistant's dependency injection container. Common services that an extension might register include will be detailed below.

When building a project, you can reference the Upgrade Assistants abstractions via

```
<PackageReference Include="Microsoft.DotNet.UpgradeAssistant.Abstractions" Version="*" />
```

This will also augment the build process so that the project will publish on builds. That publish directory is what should be used when adding an extension to Upgrade Assistant.

## Publishing to NuGet.org

Extensions for Upgrade Assistant can be published to NuGet.org and will be available to search and add to projects. In order to publish, you can run the `dotnet pack` command on the project (if it is a .NET project) or you can use `upgrade-assistant extensions create [path]` if it is simply a manifest file and config files.

Available Upgrade Assistant extensions available on NuGet can be viewed [here](https://www.nuget.org/packages?packagetype=UpgradeAssistantExtension&sortby=relevance&q=&prerel=True). You may use your own feed as well, but search is only available on NuGet.org. 


## Extension Service Providers
Any other services that might be needed by Upgrade Assistant steps (either the default steps or those added by extensions) can be registered. Extensions can register services that their own upgrade steps will need or services that will be used by other upgrade steps and Upgrade Assistant will make sure any services registered in an `IExtensionServiceProvider` implementation will be made available at runtime:

```csharp
public class TestExtension : IExtensionServiceProvider
{
    public void AddServices(IExtensionServiceCollection services)
    {
        services.Services.AddSingleton<SomeService>();
    }
}
```

### Registering Services Configuration

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
      public string Value { get; set; }
    }
}
```

In order to set these values, you can either do it via `ExtensionManifest.json`:


```json
{
  "sectionName": {
    "Value": "hello"
  }
}
```

or via the command line:

```
upgrade-assistant [...] --option "sectionName:Value=other"
```

This can then be access from within an extension in a couple of ways:

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
          // Get a merged set of options from all extensions
          IOptions<SomeOptions> options,

          // Get options from each extension as separate options
          IOptions<ICollection<SomeOptions>> collection)
        {
        }
    }
}
```

### Accessing extension files
If you would like to be able to access the backing file structure, ensure your option type implements `IFileOption`:

```csharp
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public class SomeOptions : IFileOption
    {
      public IFileProvider Files { get; set; }
    }

    public class TestService
    {
        public TestService(
          // Get options for each extension with a scoped file provider
          IOptions<ICollection<SomeOptions>> options)
        {
        }
    }
}
```

The `SomeOptions.Files` property will contain a scoped file provider to the directory of the extension to access files referenced within the options.

### Mapping custom configuration to files

Often, an extension will allow for a list of files to be used to map to other types (see the PackageUpdater or TemplateUpdater steps for full examples). Since this is a common pattern, there is a helper to enable this mapping to a resolved option:

```csharp
using System.Collections.Generic;
using Microsoft.DotNet.UpgradeAssistant.Extensions;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public class SomeOptions
    {
        public IEnumerable<string> Files { get; set; }
    }

    public class SomeOtherOption
    {
    }

    public class TestExtension : IExtensionServiceProvider
    {
        public void AddServices(IExtensionServiceCollection services)
        {
            services.AddExtensionOption<SomeOptions>("sectionName")
                .MapFiles<SomeOtherOption>(o => o.Files);
        }
    }

    public class TestService
    {
        public TestService(
          // Get the mapped options from each extension
          IOptions<ICollection<SomeOtherOption>> options)
        {
        }
    }
}
```

There is no way to access a single option, but only a collection of all the options from all the extensions. As stated above, if `SomeOtherOption : IFileOption`, then it would have access to a scoped file provider for that extension.

## Steps
Custom upgrade steps (inheriting from `UpgradeStep`) can be added to the process. Any upgrade steps registered by an `IExtensionServiceProvider` in an assembly listed in the ExtensionServiceProviders array will be included in the upgrade steps the Upgrade Assistant executes. In this way, extenders can add their own custom steps to the upgrade pipeline.

## Analyzers/code fixers
Roslyn analyzers and code fix providers. Upgrade Assistant's source updater step looks in the dependency injection container for any analyzers with associated code fix providers and will include them in the sub-steps to the source updater step. So, by registering their own Roslyn analyzers and code fix providers, extenders can customize the source update steps used by Upgrade Assistant.

Upgrade Assistant follows this naming pattern for DiagnosticID for Analyzers added: `UAXXX`. Below is a list of default analyzers and extension analyzers currently implemented. When adding a new Analyzer, pick the next value for ID and update the section below with the newly added Analyzer information.

- [Default Analyzers List](../src/extensions/default/analyzers/Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers/AnalyzerReleases.Unshipped.md)
- [.NET MAUI Extension Analyzers List](../src/extensions/maui/Microsoft.DotNet.UpgradeAssistant.Extensions.Maui/AnalyzerReleases.Unshipped.md)

## Updaters
Various services may request an implementation of `IUpdater<TUpdater>` which provides a way to update an object of type `TUpdater`. An example is a `IUpdater<ConfigFile>` that will provide updates for configuration files (`app.config`, `web.config`).

This generic parameter can be extended by any other service to provide a custom way of defining an updater. An example of this is for configuration updaters:

```csharp
public class UnsupportedSectionConfigUpdater : IUpdater<ConfigFile>
{
    public string Id => typeof(UnsupportedSectionConfigUpdater).FullName!;

    public string Title => "Title of updater";

    public string Description => "Description of updater";

    public BuildBreakRisk Risk => BuildBreakRisk.Low;

    public Task<IUpdaterResult> ApplyAsync(IUpgradeContext context, ImmutableArray<ConfigFile> inputs, CancellationToken token)
    {
      ...
    }

    public Task<IUpdaterResult> IsApplicableAsync(IUpgradeContext context, ImmutableArray<ConfigFile> inputs, CancellationToken token)
    {
      ...
    }
}
```

## Dependency analyzers
Dependency analysis can be extended in multiple ways, including code and simple configuration.

Some built-in dependency analyzers perform the following actions:
  1. Removes packages that other referenced packages depend on (transitive dependencies). PackageReference-style references only need to include top level packages so this analyzer removes package references that appear to be transitive so that only top-level dependencies are included in the csproj.
  2. If a referenced NuGet package isn't compatible with the target .NET version but a newer version of the NuGet package is, the package updater step automatically updates the version to the first major version that will work.
  3. If a `PackageMap` is defined, NuGet references will be replaced based on specific packages listed in configuration files. For example, there's a config setting that specifically indicates `System.Threading.Tasks.Dataflow` should replace `TPL.Dataflow`.

Package maps are one of the non-code ways to extend the dependency analysis step. An extension's `PackageMapPath` path can contain json files that map old packages to new ones. In each set, there are old (.NET Framework) package references which should be removed and new (.NET 6.0/Standard) package references which should be added. If a project being upgraded by the tool contains references to any of the 'NetFrameworkPackages' references from a set, those references will be removed and all of the 'NetCorePackages' references from that same set will be added. If old (NetFrameworkPackage) package references specify a version, then the references will only be replaced if the referenced version is less than or equal to that version.

By adding package map files, users can supply customized rules about which dependencies (ie NuGet package references, assembly references, etc) should be removed from upgraded projects and which dependency (if any) should replace them.

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

## Templates
The template inserter step of the Upgrade Assistant adds necessary template files to the project being upgraded. For example, when upgrading a web app, this step will add Program.cs and Startup.cs files to the project to enable ASP.NET Core startup paths. The TemplatePath property of the extension manifest points to a directory that will be probed for TemplateConfig.json files. The files define files that should be added to upgraded projects.

A template config file is a json file containing the following properties:

  1. An array of template items which lists the template files to be inserted. Each of these template items will have a path (relative to the config file) where the template file can be found, and what MSBuild 'type' of item it is (compile, content, etc.). The folder structure of template files will be preserved, so the inserted file's location relative to the project root will be the same as the template file's location relative to the config file. Each template item can also optionally include a list of keywords which are present in the template file. This list is used by the Upgrade Assistant to determine whether the template file (or an acceptable equivalent) is already present in the project. If a file with the same name as the template file is already present in the project, the template inserter step will look to see whether the already present file contains all of the listed keywords. If it does, then the file is left unchanged. If any keywords are missing, then the file is assumed to not be from the template and it will be renamed (with the pattern {filename}.old.{ext}) and the template file will be inserted instead.
  1. A dictionary of replacements. Each item in this dictionary is a key/value pair of tokens that should be replaced in the template file to customize it for the particular project it's being inserted into. MSBuild properties can be used as values for these replacements. For example, many template configurations replace 'WebApplication1' (or some similar string) with '$(RootNamespace)' so that the namespace in template files is replaced with the root namespace of the project being upgraded.
  1. There are various fields that can be used to identify the applicable language, project types, and output types so that the templates will only be applied where it matters.

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
  "TemplateOutputType": [
    "Exe"
  ],
  "TemplateLanguage": "CSharp",
  "TemplateAppliesTo": [
    "AspNetCore"
  ]
}
```
