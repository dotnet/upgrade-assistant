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
- Update the project file to be an SDK-style project
- Remove transitive NuGet package dependencies that may have been present in packages.config
- Re-target project to .NET 5.0
- Update NuGet package dependencies to versions that are compatible with .NET 5.0
- Make simple updates in C# source code to replace patterns that worked in .NET Framework with .NET 5.0 equivalents
- For some app models (like ASP.NET apps), add common template files (like startup.cs) and make simple updates based on recognized web.config or app.config values
- For projects targeting Windows, add a reference to the Microsoft.Windows.Compatibility package
- Add references to analyzers that help with upgrade, such as the Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers package

After running this tool on a solution, the solution will likely not build until the upgrade is completed manually. Analyzers added to the solution will highlight some of the remaining changes needed after the tool runs.

## Upgrade documentation

As you upgrade projects from .NET Framework to .NET 5, it will be very useful to be familiar with relevant [porting documentation](https://docs.microsoft.com/dotnet/core/porting/).

Web scenarios can be especially challenging, so it you are upgrading an ASP.NET app, be sure to read [ASP.NET Core migration documentation](https://docs.microsoft.com/aspnet/core/migration/proper-to-2x). If you are unfamiliar with ASP.NET Core, you should also read [ASP.NET Core fundamentals documentation](https://docs.microsoft.com/aspnet/core/fundamentals) to learn about important ASP.NET Core concepts (hosting, middleware, routing, etc.).

The following tutorials will give you a sense of how to upgrade ASP.NET, Windows Forms, and WPF applications to .NET 5 using the Upgrade Assistant:
- [Upgrade an ASP.NET MVC App to .NET 5 with the .NET Upgrade Assistant](https://docs.microsoft.com/en-us/dotnet/core/porting/upgrade-assistant-aspnetmvc)
- [Upgrade a Windows Forms App to .NET 5 with the .NET Upgrade Assistant](https://docs.microsoft.com/en-us/dotnet/core/porting/upgrade-assistant-winforms-framework)
- [Upgrade a WPF App to .NET 5 with the .NET Upgrade Assistant](https://docs.microsoft.com/en-us/dotnet/core/porting/upgrade-assistant-wpf-framework)

Download this free e-book on [Porting existing ASP.NET apps to .NET Core](https://aka.ms/aspnet-porting-ebook)

[![Porting existing ASP.NET apps to .NET Core by Steve "ardalis" Smith](https://user-images.githubusercontent.com/782127/108890126-2c82f680-75db-11eb-9358-dc0a5d877b6d.png)](https://aka.ms/aspnet-porting-ebook)

## Installation

### Prerequisites

1. This tool uses MSBuild to work with project files. Make sure that a recent version of MSBuild is installed. An easy way to do this is to [install Visual Studio 2019](https://visualstudio.microsoft.com/downloads/).

### Installation steps

The tool can be installed [from NuGet](https://www.nuget.org/packages/upgrade-assistant/) as a .NET CLI tool by running:

```
dotnet tool install -g upgrade-assistant
```

Similarly, because the Upgrade Assistant is installed as a .NET CLI tool, it can be easily updated from the command line:

```
dotnet tool update -g upgrade-assistant
```

If installation fails, trying running the install command with the `--ignore-failed-sources` parameter: `dotnet tool install -g upgrade-assistant --ignore-failed-sources`. Upgrade-assistant is installed as a NuGet package, so invalid or authenticated sources in [NuGet configuration](https://docs.microsoft.com/nuget/consume-packages/configuring-nuget-behavior) can cause installation problems.

To try the latest (and likely less stable) versions of the tool, CI builds are available on the dotnet-tools NuGet feed and can be installed with `dotnet tool install -g upgrade-assistant --add-source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json` or updated using the same --add-source parameter.

## Usage

### Running the tool

The usual usage of the tool is: `upgrade-assistant <Path to csproj or sln to upgrade>`

Full usage information:

```
Usage:
  upgrade-assistant [options] <project>

Arguments:
  <project>

Options:
  --skip-backup                                Disables backing up the project. This is
                                               not recommended unless the project is in
                                               source control since this tool will make
                                               large changes to both the project and
                                               source files.
  --extension <extension>                      Specifies a .NET Upgrade Assistant
                                               extension package to include. This could
                                               be an ExtensionManifest.json file, a
                                               directory containing an
                                               ExtensionManifest.json file, or a zip
                                               archive containing an extension. This
                                               option can be specified multiple times.
  -e, --entry-point <entry-point>              Provides the entry-point project to start
                                               the upgrade process.
  -v, --verbose                                Enable verbose diagnostics
  --non-interactive                            Automatically select each first option in
                                               non-interactive mode.
  --non-interactive-wait                       Wait the supplied seconds before moving
  <non-interactive-wait>                       on to the next option in non-interactive
                                               mode.
  --version                                    Show version information
  -?, -h, --help                               Show help and usage information
```

>**:warning:** The primary usage of upgrade-assistant is to be used in interactive mode, giving users control over changes/upgrades done to their projects. Usage of upgrade-assistant with --non-interactive mode can leave projects in a broken state and users are advised to use at their own discretion. **:warning:**

### Determining upgrade feasibility

Note that this tool does not (yet) advise on the feasibility or estimated cost of upgrading projects. It assumes that projects it runs on have already been reviewed and a decision taken to upgrade them to .NET 5.0.

If you're just starting to look at .NET 5.0 and would like to understand more about potential challenges in upgrading any particular project, you should begin by looking at .NET Framework dependencies the project has and third-party libraries or NuGet packages it depends on and understand whether those dependencies are likely to work on .NET 5.0. Resources that can help with that analysis include:

1. [The .NET Portability Analyzer tool](https://github.com/microsoft/dotnet-apiport)
2. [.NET Core porting documentation](https://docs.microsoft.com/dotnet/core/porting/)
3. [Documentation of features not available on .NET Core](https://docs.microsoft.com/dotnet/core/porting/net-framework-tech-unavailable)

### Extensibility
The Upgrade Assistant has an extension system that make it easy for you to customize many of the upgrade steps without having to rebuild the tool. See how you can extend the tool [here](docs/extensibility.md).

## Solution Structure

The structure of projects in the solution is organized into the following categories:

- *cli* This folder is for projects that are required for the CLI implementation of the upgrade assistant.
- *common* This folder is for common assemblies that extensions are expected to use.
- *components* This folder is for common assemblies that are not to be used in extensions, but may be used by different implementations of the tool.
- *extensions* This folder is for extensions that provide additional upgrade knowledge to the tool.
  - *analyzers* These are where custom analyzers are added. Analyzers must be separated by language if they are using language-specific syntax and cannot use any Workspace-related types. Code fixers may use workspace, and are thus kept in their own assemblies. This is because they may be hosted in different environments that may not have all language or workspace support.
- *steps* This folder contains projects for the different upgrade steps that are used. These should only use the abstraction class and should not depend on each other.

A similar structure is mirrored for the tests to identify where each of the tests will be located.

## Terminology

Concepts referred to in this repository which may have unclear meaning are explained in the following table.

| Name    | Description |
|---------|-------------|
| Step    | A step can define commands that can perform actions on the project. Each step implements `UpgradeStep`. The upgrade process comprises a series of steps that are visited in turn. Examples include the 'Update package versions step' or the 'Project backup step'|
| Command | A command is an action that can be invoked by a user. Examples include a command to apply the current step or a command to change the backup location.|
| Project Components | AppModel-specific components that a project may depend on. The most common are `WindowsDesktop` components (for WPF and WinForms scenarios) and `Web` (for ASP.NET scenarios) |

## Roadmap
Take a look at the high level overview of the roadmap for this tool and the journey to upgrade your apps from .NET Framework to .NET 5 and beyond in the [roadmap](docs/roadmap.md).

## Feedback and Issues
As you use the tool to upgrade your apps to .NET 5, please share your thoughts with us. If you have a suggestion for a new feature or idea, we want to know!
And, of course, if you find an issue or need help, please log an issue.

[Share your feedback with us or report an issue](https://github.com/dotnet/upgrade-assistant/issues)

The repo will be open for code contributions very soon.

Happy upgrading to .NET 5!
