# Upgrade Assistant

## Status

| |Build (Debug)|Build (Release)|
|---|:--:|:--:|
| ci |[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/upgrade-assistant/dotnet.upgrade-assistant?branchName=main&jobName=Windows_NT&configuration=Windows_NT%20Debug)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=953&branchName=main)|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/upgrade-assistant/dotnet.upgrade-assistant?branchName=main&jobName=Windows_NT&configuration=Windows_NT%20Release)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=953&branchName=main)|
| official | [![Build Status](https://dev.azure.com/dnceng/internal/_apis/build/status/dotnet/upgrade-assistant/dotnet-upgrade-assistant?branchName=main&stageName=Build&jobName=Windows_NT&configuration=Windows_NT%20Debug)](https://dev.azure.com/dnceng/internal/_build/latest?definitionId=949&branchName=main)|[![Build Status](https://dev.azure.com/dnceng/internal/_apis/build/status/dotnet/upgrade-assistant/dotnet-upgrade-assistant?branchName=main&stageName=Build&jobName=Windows_NT&configuration=Windows_NT%20Release)](https://dev.azure.com/dnceng/internal/_build/latest?definitionId=949&branchName=main)|
<br/>

## Overview

This project enables automation of common tasks related to upgrading .NET Framework projects to .NET 5.0. Note that this is not a complete upgrade tool and work *will* be required after using the tooling to upgrade a project.

When run on a solution, the tool will:

- Determine which projects need upgraded and recommend the order the projects should be upgraded in
- Update the project file to be an SDK-style project and re-target it to .NET 5.0
- Update NuGet package dependencies to versions that are compatible with .NET 5.0
- Remove transitive NuGet package dependencies that may have been present in packages.config
- Make simple updates in C# source code to replace patterns that worked in .NET Framework with .NET 5.0 equivalents
- For some app models (like ASP.NET apps), add common template files (like startup.cs) and make simple updates based on recognized web.config or app.config values
- For projects targeting Windows, add a reference to the Microsoft.Windows.Compatibility package
- Add references to analyzers that help with upgrade, such as the Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers package

After running this tool on a solution, the solution will likely not build until the upgrade is completed manually. Analyzers added to the solution will highlight some of the remaining changes needed after the tool runs.

## Upgrade documentation

As you upgrade projects from .NET Framework to .NET 5, it will be very useful to be familiar with relevant [porting documentation](https://docs.microsoft.com/dotnet/core/porting/).

Web scenarios can be especially challenging, so it you are upgrading and ASP.NET app, be sure to read [ASP.NET Core migration documentation](https://docs.microsoft.com/aspnet/core/migration/proper-to-2x). If you are unfamiliar with ASP.NET Core, you should also read [ASP.NET Core fundamentals documentation](https://docs.microsoft.com/aspnet/core/fundamentals) to learn about important ASP.NET Core concepts (hosting, middleware, routing, etc.).

Download this free e-book on [Porting existing ASP.NET apps to .NET Core](https://aka.ms/aspnet-porting-ebook)

[![Porting existing ASP.NET apps to .NET Core by Steve "ardalis" Smith](https://user-images.githubusercontent.com/782127/108890126-2c82f680-75db-11eb-9358-dc0a5d877b6d.png)](https://aka.ms/aspnet-porting-ebook)

## Installation

### Prerequisites

1. This tool uses MSBuild to work with project files. Make sure that a recent version of MSBuild is installed. An easy way to do this is to [install Visual Studio 2019](https://visualstudio.microsoft.com/downloads/).
1. This Upgrade Assistant depends on [try-convert](https://github.com/dotnet/try-convert). In order for the tool to run correctly, you must install the try-convert tool for converting project files to the new SDK style. If you already have try-convert installed, you may need to update it instead (since upgrade-assistant depends on version 0.7.157502 or later)
    1. To install try-convert: `dotnet tool install -g try-convert`
    1. To update try-convert: `dotnet tool update -g try-convert`

### Installation steps

The tool can be installed as a .NET CLI tool by running: `dotnet tool install -g Microsoft.DotNet.UpgradeAssistant.Cli --add-source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json`

Similarly, because the Upgrade Assistant is installed as a .NET CLI tool, it can be easily updated by running: `dotnet tool update -g Microsoft.DotNet.UpgradeAssistant.Cli --add-source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json`

Note that if you add the source to [NuGet's configuration](https://docs.microsoft.com/nuget/consume-packages/configuring-nuget-behavior) you may omit the `--add-source` parameter.

## Usage

### Running the tool

If you installed the tool using the .NET CLI, it can be run by calling `dotnet upgrade-assistant`. Otherwise, it can be run by invoking `Microsoft.DotNet.UpgradeAssistant.Cli.exe`.

The usual usage of the tool is: `dotnet upgrade-assistant <Path to csproj or sln to upgrade>`

Full usage information:

```
Usage:
  dotnet upgrade-assistant [options] <project>

Arguments:
  <project>

Options:
  --skip-backup                  Disables backing up the project. This is not
                                 recommended unless the project is in source control
                                 since this tool will make large changes to both the
                                 project and source files.
  -e, --extension <extension>    Specifies a .NET Upgrade Assistant extension package to
                                 include. This could be an ExtensionManifest.json file,
                                 a directory containing an ExtensionManifest.json file,
                                 or a zip archive containing an extension. This option
                                 can be specified multiple times.
  -v, --verbose                  Enable verbose diagnostics
  --version                      Show version information
  -?, -h, --help                 Show help and usage information
```

### Determining upgrade feasibility

Note that this tool does not (yet) advise on the feasibility or estimated cost of upgrading projects. It assumes that projects it runs on have already been reviewed and a decision taken to upgrade them to .NET 5.0.

If you're just starting to look at .NET 5.0 and would like to understand more about potential challenges in upgrading any particular project, you should begin by looking at .NET Framework dependencies the project has and third-party libraries or NuGet packages it depends on and understand whether those dependencies are likely to work on .NET 5.0. Resources that can help with that analysis include:

1. [The .NET Portability Analyzer tool](https://github.com/microsoft/dotnet-apiport)
2. [.NET Core porting documentation](https://docs.microsoft.com/dotnet/core/porting/)
3. [Documentation of features not available on .NET Core](https://docs.microsoft.com/dotnet/core/porting/net-framework-tech-unavailable)

### Troubleshooting common issues

1. If try-convert fails:
    1. Check that try-convert is installed (and that it is either located at %USERPROFILE%\.dotnet\tools\try-convert.exe or upgrade-assistant's appsettings.json file has been updated with the correct location).
    2. Check that try-convert is at least version 0.7.157502 or higher).
    3. Check whether the input project imports custom props or targets files. Try-convert doesn't support converting projects that import unknown props and targets files. Look at the output from upgrade-assistant and try-convert to see if any unrecognized imports are mentioned.

## Solution Structure

The structure of projects in the solution is organized into the following categories:

- *cli* This folder is for projects that are required for the CLI implementation of the upgrade assistant.
- *common* This folder is for common assemblies that extensions are expected to use.
- *components* This folder is for common assemblies that are not to be used in extensions, but may be used by different implementations of the tool.
- *extensions* This folder is for extensions that provide additional upgrade knowledge to the tool.
  - *analyzers* These are where custom analyzers are added. Analyzers must be separated by language if they are using language-specific syntax and cannot use any Workspace-related types. Code fixers may use workspace, and are thus kept in their own assemblies. This is because they may be hosted in different environments that may not have all language or workspace support.
- *steps* This folder contains projects for the different upgrade steps that are used. These should only use the abstraction class and should not depend on each other.

A similar structure is mirrored for the tests to identify where each of the tests will be located.

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

## Terminology

Concepts referred to in this repository which may have unclear meaning are explained in the following table.

| Name    | Description |
|---------|-------------|
| Step    | A step can define commands that can perform actions on the project. Each step implements `UpgradeStep`. The upgrade process comprises a series of steps that are visited in turn. Examples include the 'Update package versions step' or the 'Project backup step'|
| Command | A command is an action that can be invoked by a user. Examples include a command to apply the current step or a command to change the backup location.|
| Project Components | AppModel-specific components that a project may depend on. The most common are `WindowsDesktop` components (for WPF and WinForms scenarios) and `Web` (for ASP.NET scenarios) |
