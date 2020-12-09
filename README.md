# ASP.NET Migrator

## Overview

This project enables automation of common tasks related to migrating ASP.NET MVC and WebAPI projects to ASP.NET Core. Note that this is not a complete migration tool and work *will* be required after using the tooling on an ASP.NET project to complete migration.

After running this tool on a project, the project will not build until the migration is completed manually (as it will be partially migrated to .NET Core). Analyzers added to the project will highlight some of the remaining changes needed after the tool runs.

## Migration documentation

As you migrate projects from ASP.NET to ASP.NET Core, it will be very useful to be familiar with [ASP.NET Core migration documentation](https://docs.microsoft.com/aspnet/core/migration/proper-to-2x).

If you are unfamiliar with ASP.NET Core, you should also read [ASP.NET Core fundamentals documentation](https://docs.microsoft.com/aspnet/core/fundamentals) to learn about important ASP.NET Core concepts (hosting, middleware, routing, etc.).

## Installation

### Prerequisites

1. This tool uses MSBuild to work with project files. Make sure that a recent version of MSBuild is installed. An easy way to do this is to [install Visual Studio 2019](https://visualstudio.microsoft.com/downloads/).
1. This migration tool depends on [try-convert](https://github.com/dotnet/try-convert). In order for the tool to run correctly, you must install the try-convert tool for converting project files to the new SDK style:
    1. `dotnet tool install -g try-convert`

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

## Terminology

Concepts referred to in this repository which may have unclear meaning are explained in the following table.

| Name    | Description |
|---------|-------------|
| Step    | A step can define commands that can perform actions on the project. Each step implements `MigrationStep`. The migration process comprises a series of steps that are visited in turn. Examples include the 'Update package versions step' or the 'Project backup step'|
| Command | A command is an action that can be invoked by a user. Examples include a command to apply the current step or a command to change the backup location.|
