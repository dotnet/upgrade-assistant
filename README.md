# Upgrade Assistant

## Share your feedback on the .NET Upgrade Assistant!
We're interested to hearing how your experience with the .NET Upgrade Assistant has been going as you upgrade your project(s) from .NET Framework to the latest version of .NET (current, LTS, or preview).

[Share your feedback here](https://www.surveymonkey.com/r/2LBPCXH)!

## Status

| |Build (Debug)|Build (Release)|
|---|:--:|:--:|
| ci |[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/upgrade-assistant/dotnet.upgrade-assistant?branchName=main&jobName=Windows_NT&configuration=Windows_NT%20Debug)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=953&branchName=main)|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/upgrade-assistant/dotnet.upgrade-assistant?branchName=main&jobName=Windows_NT&configuration=Windows_NT%20Release)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=953&branchName=main)|
| official | [![Build Status](https://dev.azure.com/dnceng/internal/_apis/build/status/dotnet/upgrade-assistant/dotnet-upgrade-assistant?branchName=main&stageName=Build&jobName=Windows_NT&configuration=Windows_NT%20Debug)](https://dev.azure.com/dnceng/internal/_build/latest?definitionId=949&branchName=main)|[![Build Status](https://dev.azure.com/dnceng/internal/_apis/build/status/dotnet/upgrade-assistant/dotnet-upgrade-assistant?branchName=main&stageName=Build&jobName=Windows_NT&configuration=Windows_NT%20Release)](https://dev.azure.com/dnceng/internal/_build/latest?definitionId=949&branchName=main)|
<br/>

## Overview

This project and tool enables automation of common tasks related to upgrading .NET Framework projects to the latest versions of .NET (current, LTS, or Preview). See https://dotnet.microsoft.com/platform/support/policy/dotnet-core for more details on the specific versions. Note that this tool will not handle every aspect of upgrading your project(s). Manual work *will* be required after using the tool to complete the upgrade.

The tool has 2 entry points: [Analyze](#analyze-solution-prior-to-upgrade) and [Upgrade](#upgrade-solution) to assist in understanding dependencies before upgrading and then with the actual changes to your project files, code files, and dependencies. There are also several ways to add more support to the tool by adding [extensions](#solution-wide-extension-management), [experimental features](#experimental-features), and [optional features](#optional-features).

### Supported project types and languages
Currently, the tool supports the following project types:

- ASP.NET MVC
- Windows Forms
- Windows Presentation Foundation (WPF)
- Console app
- Libraries
- UWP to Windows App SDK (WinUI)
- Xamarin.Forms to .NET MAUI

The tool supports C# and Visual Basic projects.

### Analyze Solution prior to Upgrade

When run on a solution in order to analyze dependencies prior to upgrade, the tool will provide an analysis report for each of the projects in the solution containing details on:

- Package dependencies that need to be removed / added / upgraded in order to upgrade the project to chosen TFM (current, LTS, or preview)
- References that need to be removed / added / upgraded in order to upgrade the project to chosen TFM (current, LTS, or preview)
- Framework References that need to be removed / added / upgraded in order to upgrade the project to chosen TFM (current, LTS, or preview)
- Call out if there is a package upgrade across major versions that could lead towards having breaking changes.
- Unsupported API for the chosen TFM (current, LTS, or preview) used in the projects with pointers to recommended path forward if one is available.

### Upgrade Solution

When run on a solution in order to upgrade, the tool will:

- Determine which projects need upgraded and recommend the order the projects should be upgraded in
- Update the project file to be an SDK-style project
- Remove transitive NuGet package dependencies that may have been present in packages.config
- Re-target project to .NET current, LTS, or preview
- Update NuGet package dependencies to versions that are compatible with .NET current, LTS, or preview
- Make simple updates in C# source code to replace patterns that worked in .NET Framework with current, LTS, or preview equivalents
- For some app models (like ASP.NET apps), add common template files (like startup.cs) and make simple updates based on recognized web.config or app.config values
- For projects targeting Windows, add a reference to the Microsoft.Windows.Compatibility package
- Add references to analyzers that help with upgrade, such as the Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers package

After running this tool on a solution to upgrade, the solution will likely not build until the upgrade is completed manually. Analyzers added to the solution will highlight some of the remaining changes needed after the tool runs.

### Solution wide extension management

Extensions may be managed centrally for a project as described [here](docs/design/Extension_Management.md).

### Experimental features

Feature flags can be used to turn some experimental features on or off. Functionality behind feature flags may or may not be made public and are considered a way to test features that may be unstable. In order to enable a feature, set an environment variable to a semi-colon delimited list of feature names. For example: `$env:UA_FEATURES="FEATURE1;FEATURE2"`.

Current features that are available to try out include:

- `ANALYZE_BINARIES`: Enables [preview command `analyzebinaries`](./docs/binary_analysis.md) to perform .NET compatibility analysis on binary files.
- `SOLUTION_WIDE_SDK_CONVERSION`: Switches project format conversion from old style project files to SDK style to be solution wide first before any other changes to the project files.

### Optional Features

Some features within Upgrade Assistant may be enabled by adding extensions. Available extensions can be viewed on [here](https://www.nuget.org/packages?packagetype=UpgradeAssistantExtension&sortby=relevance&q=&prerel=True).

**Loose Assembly Identification**

Some projects incorporate what we call "loose assemblies" where assemblies are added directly to a repo. An experimental feature described [here](docs/design/Loose_binary_identification.md) can be used to help identify these and convert them to available NuGet packages.

## Upgrade documentation

As you upgrade projects from .NET Framework to .NET (current, LTS, or preview), it will be very useful to be familiar with relevant [porting documentation](https://docs.microsoft.com/dotnet/core/porting/).

Web scenarios can be especially challenging, so it you are upgrading an ASP.NET app, be sure to read [ASP.NET Core migration documentation](https://docs.microsoft.com/aspnet/core/migration/proper-to-2x). If you are unfamiliar with ASP.NET Core, you should also read [ASP.NET Core fundamentals documentation](https://docs.microsoft.com/aspnet/core/fundamentals) to learn about important ASP.NET Core concepts (hosting, middleware, routing, etc.).

The following tutorials will give you a sense of how to upgrade ASP.NET, Windows Forms, and WPF applications to .NET (current, LTS, or preview) using the Upgrade Assistant:
- [Upgrade an ASP.NET MVC App to .NET 6 with the .NET Upgrade Assistant](https://docs.microsoft.com/en-us/dotnet/core/porting/upgrade-assistant-aspnetmvc)
- [Upgrade a Windows Forms App to .NET 6 with the .NET Upgrade Assistant](https://docs.microsoft.com/en-us/dotnet/core/porting/upgrade-assistant-winforms-framework)
- [Upgrade a WPF App to .NET 6 with the .NET Upgrade Assistant](https://docs.microsoft.com/en-us/dotnet/core/porting/upgrade-assistant-wpf-framework)
- [Upgrade a UWP App to Windows App SDK with the .NET Upgrade Assistant](https://docs.microsoft.com/en-us/dotnet/core/porting/upgrade-assistant-uwp-framework) (This is still in preview. Read more [here](docs/uwp_support.md))

Learn more about upcoming support for Xamarin.Forms to .NET MAUI upgrades [here](docs/maui_support.md). 

Download this free e-book on [Porting existing ASP.NET apps to .NET Core](https://aka.ms/aspnet-porting-ebook).

## Installation

### Prerequisites

1. This tool uses MSBuild to work with project files. Make sure that a recent version of MSBuild is installed. An easy way to do this is to [install Visual Studio](https://visualstudio.microsoft.com/downloads/).
2. This tool requires that your project builds. This may include [installing Visual Studio](https://visualstudio.microsoft.com/downloads/) to ensure build SDKs (such as for web applications, etc) are available.

### Installation steps

The tool can be installed [from NuGet](https://www.nuget.org/packages/upgrade-assistant/) as a .NET CLI tool by running:

```
dotnet tool install -g --add-source https://api.nuget.org/v3/index.json --ignore-failed-sources upgrade-assistant
```

Similarly, because the Upgrade Assistant is installed as a .NET CLI tool, it can be easily updated from the command line:

```
dotnet tool update -g --add-source https://api.nuget.org/v3/index.json --ignore-failed-sources upgrade-assistant
```

Upgrade-assistant is installed as a NuGet package, so invalid or authenticated sources in [NuGet configuration](https://docs.microsoft.com/nuget/consume-packages/configuring-nuget-behavior) can cause installation problems.

To try the latest (and likely less stable) versions of the tool, CI builds are available on the dotnet-tools NuGet feed and can be installed with 

```
dotnet tool install -g upgrade-assistant --add-source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json
```

or to update:

```
dotnet tool update -g upgrade-assistant --add-source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json
```

## Usage

### Running the tool

#### Upgrade Path

The usage for upgrade path of the tool is: `upgrade-assistant upgrade <Path to csproj or sln to upgrade>`

Full usage information:

```
Usage:
  upgrade-assistant upgrade [options] <project>

Arguments:
  <project>

Options:
  --skip-backup                                  Disables backing up the project. This is not recommended unless the
                                                 project is in source control since this tool will make large changes
                                                 to both the project and source files.
  --extension <extension>                        Specifies a .NET Upgrade Assistant extension package to include. This
                                                 could be an ExtensionManifest.json file, a directory containing an
                                                 ExtensionManifest.json file, or a zip archive containing an extension.
                                                 This option can be specified multiple times.
  -e, --entry-point <entry-point>                Provides the entry-point project to start the upgrade process. This
                                                 may include globbing patterns such as '*' for match.
  -v, --verbose                                  Enable verbose diagnostics
  --non-interactive                              Automatically select each first option in non-interactive mode.
  --non-interactive-wait <non-interactive-wait>  Wait the supplied seconds before moving on to the next option in
                                                 non-interactive mode.
  --target-tfm-support <Current|LTS|Preview>     Select if you would like the Long Term Support (LTS), Current, or
                                                 Preview TFM. See
                                                 https://dotnet.microsoft.com/platform/support/policy/dotnet-core for
                                                 details for what these mean.
  --version                                      Show version information
  -?, -h, --help                                 Show help and usage information
```

>**:warning:** The primary usage of upgrade-assistant is to be used in interactive mode, giving users control over changes/upgrades done to their projects. Usage of upgrade-assistant with --non-interactive mode can leave projects in a broken state and users are advised to use at their own discretion. **:warning:**

#### Analyze Path

In order analyze package dependencies for a project or a solution use the analyze command with the tool like so : `upgrade-assistant analyze <Path to csproj or sln to analyze>`

Full usage information:

```
Usage:
  upgrade-assistant analyze [options] <project>

Arguments:
  <project>

Options:
  --extension <extension>                        Specifies a .NET Upgrade Assistant extension package to include. This
                                                 could be an ExtensionManifest.json file, a directory containing an
                                                 ExtensionManifest.json file, or a zip archive containing an extension.
                                                 This option can be specified multiple times.
  -v, --verbose                                  Enable verbose diagnostics
  --target-tfm-support <Current|LTS|Preview>     Select if you would like the Long Term Support (LTS), Current, or
                                                 Preview TFM. See
                                                 https://dotnet.microsoft.com/platform/support/policy/dotnet-core for
                                                 details for what these mean.
  --format  <HTML>                               Specify the format in which the analysis report will be generated. Currently supports html other than the default SARIF format.
  --version                                      Show version information
  -?, -h, --help                                 Show help and usage information
```
The output of the analyze command is a report in SARIF format. SARIF is based on JSON and can be viewed using the following viewers:

- Any text editor. 
- [VS extension for SARIF Viewer](https://marketplace.visualstudio.com/items?itemName=WDGIS.MicrosoftSarifViewer2022) for a richer experience.

Sample of the report in VS SARIF Viewer : ![Analysis Report](/docs/images/AnalysisReport.PNG)
Sample of the report in HTML format : ![Analysis Report (HTML)](/docs/images/AnalysisReportHTML.png)
### Determining upgrade feasibility

Note that this tool does not (yet) advise on the feasibility or estimated cost of upgrading projects. It assumes that projects it runs on have already been reviewed and a decision taken to upgrade them to the latest version of .NET (current, LTS, or preview).

If you're just starting to look at the latest versions of .NET (current, LTS, or preview) and would like to understand more about potential challenges in upgrading any particular project, you should begin by looking at .NET Framework dependencies the project has and third-party libraries or NuGet packages it depends on and understand whether those dependencies are likely to work on the latest version of .NET (current, LTS, or preview). Resources that can help with that analysis include:

1. [.NET Core porting documentation](https://docs.microsoft.com/dotnet/core/porting/)
1. [Documentation of features not available on .NET Core](https://docs.microsoft.com/dotnet/core/porting/net-framework-tech-unavailable)

### Authenticated NuGet sources

As part of upgrading a project, Upgrade Assistant will need to both restore the project's NuGet packages and query configured NuGet sources for information on updated packages. If a project depends on authenticated NuGet sources, some extra steps may be necessary for Upgrade Assistant to work correctly:

1. Upgrade Assistant requires that a v2 .NET Core-compatible NuGet credential provider be installed on the computer if authenticated NuGet sources are used. For example, if you are using authenticated Azure DevOps NuGet feeds, follow [these instructions](https://github.com/microsoft/artifacts-credprovider#setup) to setup a compatible credential provider. .NET Core NuGet credential providers are those installed under the netcore folder in .nuget/plugins, as explained [here](https://github.com/NuGet/Home/wiki/NuGet-cross-plat-authentication-plugin#plugin-installation-and-discovery).
2. You may need to run Upgrade Assistant in interactive mode (which is the default execution mode) in order to authenticate the NuGet sources. After authenticating once, non-interactive mode can be used as the credentials are cached.

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
| Loose assemblies | Binaries (`.dll` files) that are in the repo and not governed by a packaging system (such as NuGet) |

## Viewing log files

The project outputs a log file by default in the working directory called `upgrade-assistant.clef` that can be viewed with tools such as [Compact Log Format Viewer](https://github.com/warrenbuckley/Compact-Log-Format-Viewer) available via the [Windows Store](https://www.microsoft.com/store/apps/9N8RV8LKTXRJ?cid=storebadge&ocid=badge).

## Roadmap
Take a look at the high level overview of the roadmap for this tool and the journey to upgrade your apps from .NET Framework to the latest version of .NET (current, LTS, or preview) and beyond in the [roadmap](docs/roadmap.md).

## Engage, Contribute and Give Feedback
Some of the best ways to contribute are to use the tool to upgrade your apps to the latest version of .NET (current, LTS, or preview), file issues for feature-requests or bugs, join in design conversations, and make pull-requests. 

Check out the [contributing](/CONTRIBUTING.md) page for more details on the best places to log issues, start discussions, PR process etc.

Happy upgrading to the latest version of .NET (current, LTS, or preview)!

## Data Collection
The software may collect information about you and your use of the software and send it to Microsoft. Microsoft may use this information to provide services and improve our products and services. You may turn off the telemetry as described in the repository. There are also some features in the software that may enable you and Microsoft to collect data from users of your applications. If you use these features, you must comply with applicable law, including providing appropriate notices to users of your applications together with a copy of Microsoft's privacy statement. Our privacy statement is located [here](https://go.microsoft.com/fwlink/?LinkID=824704). You can learn more about data collection and use in the help documentation and our privacy statement. Your use of the software operates as your consent to these practices.

See the [documentation](https://aka.ms/upgrade-assistant-telemetry) for information about the usage data collection.
