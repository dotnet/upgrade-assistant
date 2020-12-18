# ASP.NET Migrator (aka "try-migrate")

## Overview

This project enables automation of common tasks related to migrating ASP.NET MVC and WebAPI projects to ASP.NET Core. Note that this is not a complete migration tool and work *will* be required after using the tooling on an ASP.NET project to complete migration.

After running this tool on a project, the project will not build until the migration is completed manually (as it will be partially migrated to .NET Core). Analyzers added to the project will highlight some of the remaining changes needed after the tool runs.

## Migration documentation

As you migrate projects from ASP.NET to ASP.NET Core, it will be very useful to be familiar with [ASP.NET Core migration documentation](https://docs.microsoft.com/aspnet/core/migration/proper-to-2x).

If you are unfamiliar with ASP.NET Core, you should also read [ASP.NET Core fundamentals documentation](https://docs.microsoft.com/aspnet/core/fundamentals) to learn about important ASP.NET Core concepts (hosting, middleware, routing, etc.).

## Installation

### Prerequisites

1. This tool uses MSBuild to work with project files. Make sure that a recent version of MSBuild is installed. An easy way to do this is to [install Visual Studio 2019](https://visualstudio.microsoft.com/downloads/).
1. This migration tool depends on [try-convert](https://github.com/dotnet/try-convert). In order for the tool to run correctly, you must install the try-convert tool for converting project files to the new SDK style. If you already have try-convert installed, you may need to update it instead (since try-migrate depends on version 0.7.157502 or later)
    1. To install try-convert: `dotnet tool install -g try-convert`
    1. To update try-convert: `dotnet tool update -g try-convert`

### Installation steps

There are two ways to install the ASP.NET Migrator tool:

1. The tool can be installed as a .NET CLI tool by running: `dotnet tool install -g try-migrate --add-source https://trymigrate.blob.core.windows.net/feed/index.json`
    1. This approach makes updating the tool easy (`dotnet tool update -g try-migrate --add-source https://trymigrate.blob.core.windows.net/feed/index.json`)
    1. Note that if you add the source to [NuGet's configuration](https://docs.microsoft.com/nuget/consume-packages/configuring-nuget-behavior) you may omit the `--add-source` parameter.
    1. Only released versions will be installed with this command; any prerelease version must be explicitly opted into by adding `--version [desired-version]` to the command.
1. A 64-bit Windows version of the tool can be downloaded [as a binary](https://mikerou.blob.core.windows.net/shared/AspNetMigrator.zip).
    1. To install the tool, simply download the zip file and extract it.
    1. This is a simple way to retrieve the tool binaries, but doesn't benefit from the .NET CLI's update infrastructure and the .NET CLI will likely need to be present anyhow to install try-convert (as mentioned in the 'prerequisites' section of these instructions).

## Usage

### Running the tool

If you installed the tool using the .NET CLI, it can be run by calling `try-migrate`. Otherwise, it can be run by invoking `AspNetMigrate.Console.exe`.

The usual usage of the tool is: `try-migrate <Path to csproj or sln to migrate>`

Full usage information:

```
Usage:
  try-migrate [options] <project>

Arguments:
  <project>

Options:
  --skip-backup                      Disables backing up the project. This is not
                                     recommended unless the project is in source control
                                     since this tool will make large changes to both the
                                     project and source files.
  -v, --verbose                      Enable verbose diagnostics
  -b, --backup-path <backup-path>    Specifies where the project should be backed up.
                                     Defaults to a new directory next to the project's
                                     directory.
  --version                          Show version information
  -?, -h, --help                     Show help and usage information
```

### Determining migration feasibility

Note that this tool does not (yet) advise on the feasibility or estimated cost of migrating projects. It assumes that projects it runs on have already been reviewed and a decision taken to migrate them to .NET Core.

If you're just starting to look at .NET Core and would like to understand more about potential challenges in migrating any particular project .NET Core, you should begin by looking at .NET Framework dependencies the project has and third-party libraries or NuGet packages it depends on and understand whether those dependencies are likely to work on .NET Core. Resources that can help with that analysis include:

1. [The .NET Portability Analyzer tool](https://github.com/microsoft/dotnet-apiport)
2. [.NET Core migration documentation](https://docs.microsoft.com/dotnet/core/porting/)
3. [Documentation of features not available on .NET Core](https://docs.microsoft.com/dotnet/core/porting/net-framework-tech-unavailable)

### Troubleshooting common issues

1. If try-convert fails:
    1. Check that try-convert is installed (and that it is either located at %USERPROFILE%\.dotnet\tools\try-convert.exe or try-migrate's appsettings.json file has been updated with the correct location).
    2. Check that try-convert is at least version 0.7.157502 or higher).
    3. Check whether the input project imports custom props or targets files. Try-convert doesn't support converting projects that import unknown props and targets files. Look at the output from try-migrate and try-convert to see if any unrecognized imports are mentioned.

## Extensibility

ASP.NET Migrator has several extension points that make it easy for users to customize many of the migration steps without having to rebuild the tool. Most of these extension points involve placing binaries or config files in directories next to the tool's location on disk. If you're not sure where the tool is installed (global .NET CLI tools can be hard to find), just run try-migrate against a project and take note of the "context base directory" that's mentioned in the very first logged message. This is the path the tool is running from and the location that all add-on paths are relative to.

### Custom source analyzers and code fix providers

The source updater step of ASP.NET Migrator uses Roslyn analyzers to identify problematic code patterns in users' projects and code fixes to automatically correct them to ASP.NET Core-compatible alternatives. Over time, the set of built-in analyzers and code fixes will continue to grow. Users may want to add their own analyzers and code fix providers as well, though, to flag and fix issues in source code specific to libraries and patterns they use in their apps.

To add their own analyzers and code fixes, users should look at the configuration setting called `SourceUpdaterStepOptions:SourceUpdaterPath` in ASP.NET Migrator's appsettings.json file. The path specified by this configuration setting is the path (relative to the app context base directory) where ASP.NET Migrator will probe for analyzers and code fix providers. Users can drop libraries containing their own analyzers and code fix providers in that folder and the tool will automatically pick them up when it starts.

Any type with a `DiagnosticAnalyzerAttribute` attribute found in or under that path is considered an analyzer and any type with an `ExportCodeFixProviderAttribute` attribute is considered a code fix provider.

### Custom NuGet package mapping configuration

The Package updater step of ASP.NET Migrator attempts to update NuGet package references to versions that will work with .NET Core. There are a few rules the migration step uses to make those updates:

  1. It removes packages that other referenced packages depend on (transitive dependencies). Try-convert moves all packages from packages.config to PackageReference references, but PackageReference-style references only need to include top level packages. The NuGet package updater step removes package references that appear to be transitive so that only top-level dependencies are included in the csproj.
  2. If a referenced NuGet package isn't compatible with the target .NET version but a newer version of the NuGet package is, ASP.NET Migrator automatically updates the version to the first major version that will work.
  3. The package updater step will replace NuGet references based on specific replacement packages listed in configuration files. For example, there's a config setting that specifically indicates System.Threading.Tasks.Dataflow should replace TPL.Dataflow.

This third type of NuGet package replacement can be customized by users. ASP.NET Migrator's `PackageUpdaterStepOptions:PackageMapPath` config setting contains the path (relative to the app context base directory) where package map configuration files are stored. Users can modify the existing json files at that path or add their own (all json files are loaded from the package map path and unioned together).

Package map config files contain different sets of NuGet package references. In each set, there are old (.NET Framework) package references which should be removed and new (.NET Core/Standard) package references which should be added. If a project being migrated by the tool contains references to any of the 'NetFrameworkPackages' references from a set, those references will be removed and all of the 'NetCorePackages' references from that same set will be added. If old (NetFrameworkPackage) package references specify a version, then the references will only be replaced if the referenced version is less than or equal to that version.

By updating these json files or adding their own, users can supply customized rules about which NuGet package references should be removed from migrated projects and which NuGet package references (if any) should replace them.

### Custom template files

The template inserter step of ASP.NET Migrator adds necessary template files to the project being migrated. For example, when migrating a web app, this step will add Program.cs and Startup.cs files to the project to enable ASP.NET Core startup paths. The `TemplateInserterStepOptions:TemplateConfigFiles` configuration setting of ASP.NET Migrator contains an array of paths (relative to the app context base directory) to config files describing the template files to be inserted. By adding their own config file (or modifying existing ones), users can customize the template files to be inserted by this migration step.

A template config file is a json file containing the following properties:

  1. An array of template items which lists the template files to be inserted. Each of these template items will have a path (relative to the config file) where the template file can be found, what MSBuild 'type' of item it is (compile, content, etc.), and whether the item should be explicitly added to the project file or if it can be omitted from the project file because it will be picked up automatically by a globbing pattern. The folder structure of template files will be preserved, so the inserted file's location relative to the project root will be the same as the template file's location relative to the config file. Each template item can also optionally include a list of keywords which are present in the template file. This list is used by ASP.NET Migrator to determine whether the template file (or an acceptable equivalent) is already present in the project. If a file with the same name as the template file is already present in the project, the template inserter step will look to see whether the already present file contains all of the listed keywords. If it does, then the file is left unchanged. If any keywords are missing, then the file is assumed to not be from the template and it will be renamed (with the pattern {filename}.old.{ext}) and the template file will be inserted instead.
  1. A dictionary of replacements. Each item in this dictionary is a key/value pair of tokens that should be replaced in the template file to customize it for the particular project it's being inserted into. MSBuild properties can be used as values for these replacements. For example, many template configurations replace 'WebApplication1' (or some similar string) with '$(RootNamespace)' so that the namespace in template files is replaced with the root namespace of the project being migrated.
  1. A boolean called UpdateWebAppsOnly that indicates whether these templates only apply to web app scenarios. Many templates that users want inserted are only needed for web apps and not for class libraries or other project types. If this boolean is true, then the template updater step will try to determine the type of project being migrated and only include the template files if the project is a web app.

If multiple template configurations attempt to insert files with the same path, the template configuration listed last in the list of config files will override earlier ones and its template file will be the one inserted.

## Terminology

Concepts referred to in this repository which may have unclear meaning are explained in the following table.

| Name    | Description |
|---------|-------------|
| Step    | A step can define commands that can perform actions on the project. Each step implements `MigrationStep`. The migration process comprises a series of steps that are visited in turn. Examples include the 'Update package versions step' or the 'Project backup step'|
| Command | A command is an action that can be invoked by a user. Examples include a command to apply the current step or a command to change the backup location.|
