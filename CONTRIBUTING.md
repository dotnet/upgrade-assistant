# Contributing

One of the easiest ways for you to contribute is to participate in discussions on GitHub issues. You can also contribute by submitting pull requests with code changes.

## General feedback and discussions?
Start a discussion on the [repository issue tracker](https://github.com/dotnet/upgrade-assistant/issues).

## Bugs and feature requests?
For non-security related bugs, please [log a new issue](https://github.com/dotnet/upgrade-assistant/issues) or simply click [this link](https://github.com/dotnet/upgrade-assistant/issues/new?assignees=&labels=bug&template=20_bug_report.md).

## How to submit a PR

We are always happy to see PRs from community members both for bug fixes as well as new features.
To help you be successful we've put together a few simple rules to follow when you prepare to contribute to our codebase:

**Finding an issue to work on**

  We've created a separate bucket of issues, which would be great candidates for community members to contribute to. We mark these issues with the `help wanted` label. You can find all these issues [here](https://github.com/dotnet/upgrade-assistant/issues?q=is%3Aissue+is%3Aopen+label%3A%22help+wanted%22).
  
Within that set, we have additionally marked issues which are good candidates for first-time contributors. Those do not require too much familiarity with the codebase and are more novice-friendly. Those are marked with `good first issue` label. The full list of such issues can be found [here](https://github.com/dotnet/upgrade-assistant/issues?q=is%3Aissue+is%3Aopen+label%3A%22good+first+issue%22).
  
If there is some other area not included here where you want to contribute to, first open an issue to describe the problem you are trying to solve and state that you're willing to contribute a fix for it. We will then discuss in that issue, if this is an issue we would like addressed and the best approach to solve it.
  
**Before writing code**

  This can save you a lot of time. We've seen PRs where customers solve an issue in a way which either wouldn't fit into upgrade-assistant because of how it's designed or would change upgrade-assistant in a way which is not something we'd like to do. To avoid these situations, we encourage customers to discuss the preferred design with the team first. To do so, file a new `design proposal` issue, link to the issue you'd like to address and provide detailed information about how you'd like to solve a specific problem. We triage issues periodically and it will not take long for a team member to engage with you on that proposal.
  When you get an agreement from our team members that the design proposal you have is solid, then go ahead and prepare the PR.
  To file a design proposal, look for the relevant issue in the `New issue` page or simply click [this link](https://github.com/dotnet/upgrade-assistant/issues/new?assignees=&labels=design-proposal&template=10_design_proposal.md):
  ![image](https://user-images.githubusercontent.com/34246760/107969904-41b9ae80-6f65-11eb-8b84-d15e7d94753b.png)
  
**Before submitting the pull request**

Before submitting a pull request, make sure that it checks the following requirements:

- You find an existing issue with the "help-wanted" label or discuss with the team to agree on adding a new issue with that label
- You post a high-level description of how it will be implemented, and receive a positive acknowledgement from the team before getting too committed to the approach or investing too much effort implementing it
- You add test coverage following existing patterns within the codebase
- Your code matches the existing syntax conventions within the codebase
- Your PR is small, focused, and avoids making unrelated changes
  
If your pull request contains any of the below, it's less likely to be merged:

- Changes that break existing functionality.
- Changes that are only wanted by one person/company. Changes need to benefit a large enough portion of upgrade-assistant users.
- Changes that add entirely new feature areas without prior agreement
- Changes that are mostly about refactoring existing code or code style
- Very large PRs that would take hours to review (remember, we're trying to help lots of people at once). For larger work areas, please discuss with us to find ways of breaking it down into smaller, incremental pieces that can go into separate PRs.

**During pull request review**
A core contributor will review your pull request and provide feedback. To ensure that there is not a large backlog of inactive PRs, the pull request will be marked as stale after two weeks of no activity. After another four days, it will be closed.

## Architecture

### Startup and hosting

Upgrade Assistant is a .NET 5 console application that uses `System.CommandLine` APIs to [parse command line arguments](src/cli/Microsoft.DotNet.UpgradeAssistant.Cli/Program.cs) and then executes commands. Logging is done using `Microsoft.Extensions.Logging` APIs with a Serilog logging provider writing logs to the console.

Upgrade Assistant's most common command, currently, is the [`ConsoleUpgradeCommand`](src/cli/Microsoft.DotNet.UpgradeAssistant.Cli/Commands/Upgrade/ConsoleUpgradeCommand.cs). This command walks through a series of 'upgrade steps' to help move a user's project or solution from .NET Framework to .NET Core/5/6. `ConsoleUpgradeCommand` use a `Microsoft.Extensions.Hosting` [generic host](https://docs.microsoft.com/dotnet/core/extensions/generic-host) to register and execute upgrade services. The host uses the [`ConsoleRunner`](src/cli/Microsoft.DotNet.UpgradeAssistant.Cli/ConsoleRunner.cs) hosted service to execute the upgrade command when the host starts. All services are resolved from the [dependency injection container](https://docs.microsoft.com/dotnet/core/extensions/dependency-injection) setup by the command, which makes it easy for extensions to register services in addition to those registered by default. Key services for the upgrade command are described in the following sections.

### Core upgrade components

- [`UpgraderManager`](src/components/Microsoft.DotNet.UpgradeAssistant/UpgraderManager.cs) is the central Upgrade Assistant component responsible for managing the list of upgrade steps and progressing through them one at a time.
  - When `UpgraderManager` identifies the next applicable step (its primary function), it iterates through all registered upgrade steps in order. It disregards any upgrade steps that return false for `IsApplicableAsync` or that have a status of complete or skipped. If it encounters a step with a status of 'unknown,' it initializes the step. As soon as the manager comes to an applicable incomplete un-skipped step, it returns it as the next step. The manager will evaluate sub-steps and return them as the next step before evaluating the parent step (though parent steps are initialized first if they are in an unknown state).
    - Every time `UpgradeManager.GetNextStepAsync` is called, the `IsApplicableAsync` method of the upgrade steps will be checked and the steps' status may be reset if their `ShouldReset` property returns true (by default this happens if the current project has changed). This means that steps can be 'repeated' by changing their `ShouldReset` step to reset them in cases where the step should be repeated. It also means that `IsApplicableAsync` must execute quickly (despite being async) because it is called frequently.
- The upgrade manager uses an [`IUpgradeStepOrderer`](src/common/Microsoft.DotNet.UpgradeAssistant.Abstractions/IUpgradeStepOrderer.cs) to order the various implementations of [`UpgradeStep`](src/common/Microsoft.DotNet.UpgradeAssistant.Abstractions/UpgradeStep.cs) that are registered (both by built-in extensions and extensions added by the user). The default version of `IUpgradeStepOrderer` ([`UpgradeStepOrderer`](src/components/Microsoft.DotNet.UpgradeAssistant/UpgradeStepOrderer.cs)) uses [Kahn's algorithm](https://en.wikipedia.org/wiki/Topological_sorting#Kahn's_algorithm) to order all registered upgrade steps such that their `DependsOn` and `DependencyOf` properties are satisfied. These properties allow upgrade steps to specify other upgrade steps by name that they need to either run before or after. Dependencies are specified by ID strings so that steps don't actually need to reference other steps' binaries to indicate that they should run before or after them. If an upgrade step lists a step in its `DependsOn` property, then the depended-upon step must be present and ordered before this step. If an upgrade step lists a step in its `DependencyOf` property, that other step does *not* need to be present but, if it is, it must be ordered after this step.
- [`IUpgradeContextFactory`](src/common/Microsoft.DotNet.UpgradeAssistant.Abstractions/IUpgradeContextFactory.cs) is responsible for creating new instances of [`IUpgradeContext`](src/common/Microsoft.DotNet.UpgradeAssistant.Abstractions/IUpgradeContext.cs) for the Upgrade Assistant to use. The upgrade context (described below) is the primary object storing the current state of the upgrade proces. It includes information about the current entry point and current projects being upgraded and APIs for accessing `IProject` and `IProjectFile` abstractions for the projects being upgraded which allow querying or modifying the projects' properties.
- [`IUpgradeStateManager`](src/common/Microsoft.DotNet.UpgradeAssistant.Abstractions/IUpgradeStateManager.cs) (implemented, by default, by [`FileUpgradeStateFactory`](src/cli/Microsoft.DotNet.UpgradeAssistant.Cli/FileUpgradeStateFactory.cs)) is responsible for storing and reading state that is preserved between runs of Upgrade Assistant in a given directory. The state file that it reads from and writes to disk (.upgrade-assistant) contains the current project being upgraded, the current entry point, and a property bag that can hold arbitrary state (but is currently only used to store the base backup location a user prefers). This state can be useful for Upgrade Assistant to start up more quickly but should not be *necessary*.
- Upgrade startup types (implementing [`IUpgradeStartup`](src/common/Microsoft.DotNet.UpgradeAssistant.Abstractions/IUpgradeStartup.cs)) are components that need to be executed once as Upgrade Assistant starts up to run pre-commands or otherwise configure settings that will be needed by other components as the tool executes. These startup tasks are run by the `ConsoleRunner` prior to creating the upgrade context or initializing or running any upgrade steps. Current built-in startup implementations include:
  - [`MSBuildRegistrationStartup`](src/components/Microsoft.DotNet.UpgradeAssistant.MSBuild/MSBuildRegistrationStartup.cs) which registers the MSBuild instance on the user's computer that will be used while running Upgrade Assistant. Upgrade Assistant does not ship with any MSBUild or NuGet binaries. Instead, the versions of these binaries present on the user's machine are used so that the build processes executed during Upgrade Assistant resemble the user's typical building of their project as much as possible.
  - [`NuGetCredentialsStartup`](src/components/Microsoft.DotNet.UpgradeAssistant.MSBuild/NuGetCredentialsStartup.cs) configures NuGet credentials according to any NuGet auth extensions present on the user's machine so that authenticated NuGet feeds can be used during Upgrade Assistant's execution.
  - [`ConsoleFirstTimeUserNotifier`](src/cli/Microsoft.DotNet.UpgradeAssistant.Cli/ConsoleFirstTimeUserNotifier.cs) displays one-time messages to users as the tool starts up if they haven't run Upgrade Assistant before.
  - [`UsedCommandTelemetry`](src/cli/Microsoft.DotNet.UpgradeAssistant.Cli/UsedCommandTelemetry.cs) reports anonymous telemetry on the command used to start Upgrade Assistant.

### Upgrade state

Throughout the upgrade, the current state of the solution being upgraded and Upgrade Assistant's current status is stored in an [`IUpgradeContext`](src/common/Microsoft.DotNet.UpgradeAssistant.Abstractions/IUpgradeContext.cs) object that is provided to most calls to individual upgrade steps or other components. Important state stored in `IUpgradeContext` includes:

- All projects in the solution being upgraded.
- Which project in the solution is the entry point for this execution of Upgrade Assistant and which project (if any) is currently being upgraded.
- The current step Upgrade Assistant will apply next.

The context also allows components with access to it to take several important actions:

- Querying information about loaded projects.
- Update the projects being upgraded, either by modifying the project files or by accessing the files in the project and modifying them.
- Reloading the workspace (solution/project being upgraded).

Access to project files is abstracted behind the [`IProject`](src/common/Microsoft.DotNet.UpgradeAssistant.Abstractions/IProject.cs) and [`IProjectFile`](src/common/Microsoft.DotNet.UpgradeAssistant.Abstractions/IProjectFile.cs) interfaces, so interacting with the projects is simple. The default implementation of `IUpgradeContext` ([`MSBuildWorkspaceUpgradeContext`](src/components/Microsoft.DotNet.UpgradeAssistant.MSBuild/MSBuildWorkspaceUpgradeContext.cs)) implements the project-related interfaces behind the scenes with MSBuild APIs.

Most of the data stored in `IUpgradeContext` is meant to be temporary and is only stored until Upgrade Assistant exits. The exceptions are the current project being upgraded, the entry point project, and any context properties explicitly marked for persistence. These data are stored in a state file on-disk (.upgrade-assistant) by the `IUpgradeStateManager` until upgrade of the solution is complete (running the `FinalizeUpgradeStep` step removes the .upgrade-assistant file).

### Upgrade steps

- Upgrade steps (deriving from [`UpgradeStep`](./src/common/Microsoft.DotNet.UpgradeAssistant.Abstractions/UpgradeStep.cs)) are the steps that tool will progress through as it helps users upgrade to .NET Core.
  - Important members of `UpgradeStep` to understand include:
    - `IsApplicableImplAsync` is used to determine whether the upgrade step makes sense to initialize and run on a given project. It should run quickly and is used to determine whether the step should be shown in the list of upgrade steps to be applied for a given upgrade context. This method is run frequently (and before step initialization) to determine whether or not the step should show up in the list of steps shown to the user.
    - `InitializeImplAsync` is used to prepare the step to execute. It is responsible for determining whether the step is needed or not (which is separate from whether the step applies to the current project type at all) and returns a value indicating whether the step is already done or if it needs applied. The initialize method is also responsible for preparing state so that it's ready to run the apply action when the user chooses to move forward. The initialize method is called infrequently - only immediately before an upgrade step will be the next to be upgraded. Parent steps are initialized before their children.
    - `ApplyImplAsync` actually executes the upgrade step and makes whatever changes are necessary for the given upgrade context. This method returns a value indicating the new state of the step (which should typically be either complete or failed).
    - `ShouldReset` determines whether the upgrade step should be reset (so that it can be run again). This method is called frequently (every time the tool identifies the next step to run) and has a base implementation that returns true if the current project in the upgrade context has changed. This behavior is probably right for many project-level upgrade steps but can be overridden if it doesn't make sense for a particular step.
    - `Reset` is called by the `UpgraderManager` if `ShouldReset` returns true and is responsible for resetting the step's state to uninitialized so that it's ready to run again. There is a base implementation that resets common upgrade step state, but the method should be overridden if an upgrade step includes additional local state that would need reset between applications.
    - `SubSteps` are UpgradeSteps that run as part of applying this step. They are shown nested under their parent in the UI. The parent step will be initialized first (so it can setup shared state the children may need) but sub-steps are applied before their parent.
- Upgrade steps defined in the default extensions include:
  - [`BackupStep`](src/steps/Microsoft.DotNet.UpgradeAssistant.Steps.Backup/BackupStep.cs) runs early in the upgrade process and backs up the input project or solution to another folder in case the user later wants to roll back to the state the input code was in prior to running Upgrade Assistant.
  - [`EntryPointSelectionStep`](src/steps/Microsoft.DotNet.UpgradeAssistant.Steps.Solution/EntrypointSelectionStep.cs) prompts the user to select which project is the head project in the case of multi-project solutions. This data is used to order the projects and recommend which should be upgraded first. It also determines which projects will be upgraded since large solutions may only need a subset of their projects upgraded to get a particular head project working.
  - [`CurrentProjectSelectionStep`](src/steps/Microsoft.DotNet.UpgradeAssistant.Steps.Solution/CurrentProjectSelectionStep.cs) prompts the user to choose which project to upgrade first in a multi-project solution since Upgrade Assistant only upgrades a single project at a time.
    - This step uses registered [`IUpgradeReadyCheck`](src/common/Microsoft.DotNet.UpgradeAssistant.Abstractions/IUpgradeReadyCheck.cs) implementations to validate that projects are able to be meaningfully upgraded by Upgrade Assistant and can be successfully loaded. There are some types of projects that Upgrade Assistant doesn't yet support (projects using multi-targeting, for example) and these ready checks allow those cases to be identified before the tool begins upgrading a project so that the project can be marked as unsupported and skipped.
    - This step orders the projects from lowest level dependencies up to top-level projects using post-order traversal extension methods so that it can recommend a useful order to upgrade projects.
  - [`TryConvertProjectConverterStep`](src/steps/Microsoft.DotNet.UpgradeAssistant.Steps.ProjectFormat/TryConvertProjectConverterStep.cs) uses the try-convert tool (packaged with Upgrade Assistant at build time) to convert the current project to an SDK-style project and updates package references to use `<PackageReference>` format instead of using packages.config.
  - [`SetTFMStep`](src/steps/Microsoft.DotNet.UpgradeAssistant.Steps.ProjectFormat/SetTFMStep.cs) updates the current project to us an updated .NET Core or .NET Standard TFM based on the user's preference for LTS/Current/Preview and data from the [`ITargetFrameworkSelector`](src/common/Microsoft.DotNet.UpgradeAssistant.Abstractions/ITargetFrameworkSelector.cs).
    - [`ITargetFrameworkSelector`](src/common/Microsoft.DotNet.UpgradeAssistant.Abstractions/ITargetFrameworkSelector.cs) implementations determine the recommended TFM for given input projects. The default implementation ([`TargetFrameworkSelector`](src/components/Microsoft.DotNet.UpgradeAssistant/TargetFramework/TargetFrameworkSelector.cs)) chooses .NET Standard 2.0 whenever possible (since it is the most portable target for modern development) and uses a series of [`ITargetFrameworkSelectorFilter`](src/common/Microsoft.DotNet.UpgradeAssistant.Abstractions/ITargetFrameworkSelectorFilter.cs) implementations to update this baseline until the correct TFM is found. These filters (which can be easily added to by registering additional filters in DI) look at factors like project output type, dependencies' TFMs, and project properties. `ITargetFrameworkSelectorFilter` implementations in the default extensions include:
      - [`ExecutableTargetFrameworkSelectorFilter`](src/components/Microsoft.DotNet.UpgradeAssistant/TargetFramework/ExecutableTargetFrameworkSelectorFilter.cs) ensures that projects built as exes build using an executable .NET target (.NET Core 3.1, .NET 5, or .NET 6, depending on the user's preferences) rather than .NET Standard.
      - [`WebProjectTargetFrameworkSelectorFilter`](src/components/Microsoft.DotNet.UpgradeAssistant/TargetFramework/WebProjectTargetFrameworkSelectorFilter.cs) ensures that web projects build against executable .NET targets since ASP.NET and ASP.NET Core apps should target .NET Core 3.1, .NET 5, or .NET 6 after upgrade rather than .NET Standard.
      - [`DependencyMinimumTargetFrameworkSelectorFilter`](src/components/Microsoft.DotNet.UpgradeAssistant/TargetFramework/DependencyMinimumTargetFrameworkSelectorFilter.cs) ensures the TFM a project is upgraded to is not less than the TFMs of that project's dependencies. So, for example, a project that has dependencies built against .NET 5 must target at least .NET 5 (not 3.1 or netstandard2.0) itself.
      - [`WindowsSdkTargetFrameworkSelectorFilter`](src/components/Microsoft.DotNet.UpgradeAssistant/TargetFramework/WindowsSdkTargetFrameworkSelectorFilter.cs) ensures that projects with Windows-specific dependencies (WinForms, WPF, etc.) use a TFM with the -windows target.
      -[ `MyTypeTargetFrameworkSelectorFilter`](src/extensions/vb/Microsoft.DotNet.UpgradeAssistant.Extensions.VisualBasic/MyTypeTargetFrameworkSelectorFilter.cs) ensures that VB projects using the MyType node upgrade to at least net5.0-windows.
  - [`PackageUpdaterStep`](src/steps/Microsoft.DotNet.UpgradeAssistant.Steps.Packages/PackageUpdaterStep.cs) uses registered [`IDependencyAnalyzer`](src/common/Microsoft.DotNet.UpgradeAssistant.Abstractions/Dependencies/IDependencyAnalyzer.cs) instances to apply upgrades based on the project's dependencies. Extensions can implement their own `IDependencyAnalyzer` implementations, but default ones include (among others) trimming transitive package dependencies, updating versions of NuGet dependencies to versions supported by .NET Core/standard, and updating references based on configurable mapping files.
    - DependencyAnalzyers are covered in more depth in [extensibility dependency analyzer docs](./docs/extensibility.md#dependency-analyzers).
  - [`PackageUpdaterPreTFMStep`](src/steps/Microsoft.DotNet.UpgradeAssistant.Steps.Packages/PackageUpdaterPreTFMStep.cs) is just like `PackageUpdaterStep` except that it's run prior to updating the project's TFM, so it will remove transitive dependencies but likely won't change any package versions. This step is meant as a way to clean up package references for users who want to modernize their project files without completely upgrading to .NET Core or .NET 5/6.
  - [`TemplateInserterStep`](src/steps/Microsoft.DotNet.UpgradeAssistant.Steps.Templates/TemplateInserterStep.cs) adds template files appropriate for the project type to the project being upgraded if they don't exist yet. Currently this only adds files for web projects and will add startup and program source files along with appsettings.json and appsettings.development.json configuration files. The files to be inserted are read from extension configuration and can be changed or added to by extensions.
  - [`SourceUpdaterStep`](src/steps/Microsoft.DotNet.UpgradeAssistant.Steps.Source/SourceUpdaterStep.cs) uses Roslyn analyzers and code fix providers to identify code patterns in C# and VB files that will need updated as part of the upgrade process. The source updater step will have sub-steps for applicable code fix providers to automatically fix the diagnostics. The parent source updater step will display any diagnostics that no code fix providers were available to address as it executes (after the sub-steps).
  - [`ConfigUpdaterStep`](src/steps/Microsoft.DotNet.UpgradeAssistant.Steps.Configuration/ConfigUpdaterStep.cs) uses registered `IUpdater<ConfigFile>` instances to apply project-level upgrades based on the contents of web.config and app.config files. For example, default sub-steps will migrate over app settings and connection strings, comment out unsupported config sections, and migrate over namespaces to be auto-imported into Razor views.
    - [`IUpdater<ConfigFile>`](src/common/Microsoft.DotNet.UpgradeAssistant.Abstractions/IUpdater.cs) is covered in more depth in [extensibility updater docs](.docs/extensibility.md#updaters).
  - [`RazorUpdaterStep`](src/extensions/web/Microsoft.DotNet.UpgradeAssistant.Steps.Razor/RazorUpdaterStep.cs) updates Razor views (cshtml files) in the project. It applies any `IUpdater<RazorCodeDocument>` updaters present in sub-steps. Out-of-the-box, the updaters included will:
    - Apply the same analyzers and code fix providers that the source updater uses on C# and VB files to cshtml.
    - Replace @helper syntax with local methods.
    - [`IUpdater<RazorCodeDocument>`](src/common/Microsoft.DotNet.UpgradeAssistant.Abstractions/IUpdater.cs) is covered in more depth in [extensibility updater docs](.docs/extensibility.md#updaters).
  - [`NextProjectStep`](src/steps/Microsoft.DotNet.UpgradeAssistant.Steps.Solution/NextProjectStep.cs) runs after upgrading a specific projects is complete and clears the current project from the upgrade context. This will cause most project-specific steps to reset their state the next time the UpgraderManager checks their applicability, causing them to run again on the next project the user selects.
  - [`FinalizeSolutionStep`](src/steps/Microsoft.DotNet.UpgradeAssistant.Steps.Solution/FinalizeSolutionStep.cs) is intended to run as the last step of the upgrade process and performs cleanup activities such as deleting the .upgrade-assistant state file.

### Extensibility

Upgrade Assistant uses an extension model that makes it possible for add-ons to modify the behavior of existing steps and services or add their own. This is done by allowing extensions to both register services in the tool's dependency injection container and by allowing extension options to be registered which can configure services while also allowing other extensions to modify the options. Upgrade Assistant's extension architecture is covered in detail in dedicated [extensibility documentation](./docs/extensibility.md).

### Diagram

The architectural components described above are shown in this architecture diagram of Upgrade Assistant's logical components:

![Component architecture](docs/images/component-architecture.png)

### Project architecture validation

The architecture of project is enforced with validation diagrams and will be run automatically at build. See the `eng/DependencyValidation` project to update or adjust this diagram. The following project architecture is currently enforced:

![Dependency diagram](docs/images/dependency-validation.png)

To disable validation for a specific project, set `ValidateLayerDiagram=false`. This is done currently for tests as they are not in the diagram.

## Resources to help you get started

Here are some resources to help you get started on how to contribute code or new content.

* Look at the [Contributor documentation](/README.md) to get started on building the source code on your own.
* ["Help wanted" issues](https://github.com/dotnet/upgrade-assistant/labels/help%20wanted) - these issues are up for grabs. Comment on an issue if you want to create a fix.
* ["Good first issue" issues](https://github.com/dotnet/upgrade-assistant/labels/good%20first%20issue) - we think these are a good for newcomers.
* [Best Practices for Roslyn Analyzers and Code Fixers](./docs/roslyn_best_practices.md) - our goal is to build analyzers that are performant and available for both C# and Visual Basic by default. 

### Identifying the scale

If you would like to contribute to upgrade-assistant, first identify the scale of what you would like to contribute. If it is small (grammar/spelling or a bug fix) feel free to start working on a fix. If you are submitting a feature or substantial code contribution, please discuss it with the team and ensure it follows the product roadmap. You might also read these two blogs posts on contributing code: [Open Source Contribution Etiquette](http://tirania.org/blog/archive/2010/Dec-31.html) by Miguel de Icaza and [Don't "Push" Your Pull Requests](https://www.igvita.com/2011/12/19/dont-push-your-pull-requests/) by Ilya Grigorik. All code submissions will be rigorously reviewed and tested further by the upgrade-assistant team, and only those that meet an extremely high bar for both quality and design/roadmap appropriateness will be merged into the source.

### Submitting a pull request

You will need to sign a [Contributor License Agreement](https://cla.dotnetfoundation.org/) when submitting your pull request. To complete the Contributor License Agreement (CLA), you will need to follow the instructions provided by the CLA bot when you send the pull request. This needs to only be done once for any .NET Foundation OSS project.

If you don't know what a pull request is read this article: https://help.github.com/articles/using-pull-requests. Make sure the repository can build and all tests pass. Familiarize yourself with the project workflow and our coding conventions. For general coding guidelines, see [here](https://github.com/dotnet/aspnetcore/wiki/Engineering-guidelines#coding-guidelines).

### Tests

[Tests](/tests) in upgrade-assistant follow the following pattern:

- Testing Framework used is [XUnit](https://docs.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-dotnet-test).
- Mocking Framework used is [Moq](https://github.com/Moq/moq4) (with [AutoMock](https://autofaccn.readthedocs.io/en/latest/integration/moq.html)).
- Data generation Framework used is [AutoFixture](https://github.com/AutoFixture/AutoFixture/wiki/Cheat-Sheet).

Tests need to be provided for every bug/feature(except docs or samples) that is completed.

### Feedback

Your pull request will now go through extensive checks by the subject matter experts on our team. Please be patient while upgrade-assistant team gets through it. Update your pull request according to feedback until it is approved by one of the upgrade-assistant team members. Once the PR is approved, one of the upgrade-assistant team members will merge your PR into the repo.

### Dev Environment FAQ
The tool may produce long file paths during build, in order to not run into PathTooLongException either change the LongPathsEnabled setting under registry or build upgrade-assistant from a folder location with a shorter path.

## Code of conduct
See [CODE-OF-CONDUCT.md](./CODE-OF-CONDUCT.md)
