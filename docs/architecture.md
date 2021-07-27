# Upgrade Assistant architeture

## Architectural components

To make sure that documentation is fresh, most explanations of Upgrade Assistant's architecture reside in comments which version with the code. Below are the key components to be familiar with in Upgrade Assistant with links to their source files where in-code comments will provide more details on architectural decisions.

### Startup and hosting

- [`Program`](../src/cli/Microsoft.DotNet.UpgradeAssistant.Cli/Program.cs) is Upgrade Assistant's entry point. From there, command line arguments are parsed and user commands are executed.
- [`ConsoleUpgradeCommand`](../src/cli/Microsoft.DotNet.UpgradeAssistant.Cli/Commands/Upgrade/ConsoleUpgradeCommand.cs) is the command that runs the upgrade workflow. It creates a `Microsoft.Extensions.Hosting` [generic host](https://docs.microsoft.com/dotnet/core/extensions/generic-host) to register and execute upgrade services. All services are resolved from the [dependency injection container](https://docs.microsoft.com/dotnet/core/extensions/dependency-injection) setup by the command, which makes it easy for extensions to register services in addition to those registered by default.
- [`ConsoleUpgrade`](../src/cli/Microsoft.DotNet.UpgradeAssistant.Cli/Commands/Upgrade/ConsoleUpgrade.cs) is the `IAppCommand` that actually runs the REPL loop to upgrade the user's project.
- [`IUpgradeStartup`](../src/common/Microsoft.DotNet.UpgradeAssistant.Abstractions/IUpgradeStartup.cs) implementations are executed once as Upgrade Assistant starts up to run pre-commands or otherwise configure settings that will be needed by other components as the tool executes. Current built-in startup implementations include:
  - [`MSBuildRegistrationStartup`](../src/components/Microsoft.DotNet.UpgradeAssistant.MSBuild/MSBuildRegistrationStartup.cs) which registers the MSBuild instance on the user's computer that will be used while running Upgrade Assistant.
  - [`NuGetCredentialsStartup`](../src/components/Microsoft.DotNet.UpgradeAssistant.MSBuild/NuGetCredentialsStartup.cs) configures NuGet credentials according to any NuGet auth extensions present on the user's machine so that authenticated NuGet feeds can be used during Upgrade Assistant's execution.
  - [`ConsoleFirstTimeUserNotifier`](../src/cli/Microsoft.DotNet.UpgradeAssistant.Cli/ConsoleFirstTimeUserNotifier.cs) displays one-time messages to users as the tool starts up if they haven't run Upgrade Assistant before.
  - [`UsedCommandTelemetry`](../src/cli/Microsoft.DotNet.UpgradeAssistant.Cli/UsedCommandTelemetry.cs) reports anonymous telemetry on the command used to start Upgrade Assistant.

### Core upgrade components

- [`UpgraderManager`](../src/components/Microsoft.DotNet.UpgradeAssistant/UpgraderManager.cs) is the central Upgrade Assistant component responsible for managing the list of upgrade steps and progressing through them one at a time.
- [`UpgradeStepOrderer`](../src/components/Microsoft.DotNet.UpgradeAssistant/UpgradeStepOrderer.cs) is the service the upgrader manager uses to order steps based on their `DependsOn` and `DependencyOf` properties.

### Upgrade state

- [`IUpgradeContext`](../src/common/Microsoft.DotNet.UpgradeAssistant.Abstractions/IUpgradeContext.cs) is the primary type used to store the current state of the upgrade process. It provides access to information about the projects being upgraded and allows callers to query information about the projects or update the projects.
- [`IProject`](../src/common/Microsoft.DotNet.UpgradeAssistant.Abstractions/IProject.cs) and [`IProjectFile`](../src/common/Microsoft.DotNet.UpgradeAssistant.Abstractions/IProjectFile.cs) abstractions are used for accessing projects in the user's solution. The default implementation of `IUpgradeContext` ([`MSBuildWorkspaceUpgradeContext`](../src/components/Microsoft.DotNet.UpgradeAssistant.MSBuild/MSBuildWorkspaceUpgradeContext.cs)) implements the project-related interfaces behind the scenes with MSBuild APIs.
- [`IUpgradeStateManager`](../src/common/Microsoft.DotNet.UpgradeAssistant.Abstractions/IUpgradeStateManager.cs) (implemented, by default, by [`FileUpgradeStateFactory`](../src/cli/Microsoft.DotNet.UpgradeAssistant.Cli/FileUpgradeStateFactory.cs)) persists select data from the upgrade context to disk until the upgrade is complete. This state can be useful for Upgrade Assistant to start up more quickly but should not be *necessary*.

### Upgrade steps

- [`UpgradeStep`](../src/common/Microsoft.DotNet.UpgradeAssistant.Abstractions/UpgradeStep.cs) is the base type that all upgrade steps derive from. Upgrade steps defined in the default extensions include:
  - [`BackupStep`](../src/steps/Microsoft.DotNet.UpgradeAssistant.Steps.Backup/BackupStep.cs) runs early in the upgrade process and backs up the input project or solution to another folder in case the user later wants to roll back to the state the input code was in prior to running Upgrade Assistant.
  - [`EntryPointSelectionStep`](../src/steps/Microsoft.DotNet.UpgradeAssistant.Steps.Solution/EntrypointSelectionStep.cs) prompts the user to select which project is the head project in the case of multi-project solutions.
  - [`CurrentProjectSelectionStep`](../src/steps/Microsoft.DotNet.UpgradeAssistant.Steps.Solution/CurrentProjectSelectionStep.cs) orders the projects to be upgraded and prompts the user to choose which project to upgrade first in a multi-project solution since most upgrade steps only upgrade a single project at a time.
  - [`TryConvertProjectConverterStep`](../src/steps/Microsoft.DotNet.UpgradeAssistant.Steps.ProjectFormat/TryConvertProjectConverterStep.cs) uses the try-convert tool (packaged with Upgrade Assistant at build time) to convert the current project to an SDK-style project and updates package references to use `<PackageReference>` format instead of using packages.config.
  - [`SetTFMStep`](../src/steps/Microsoft.DotNet.UpgradeAssistant.Steps.ProjectFormat/SetTFMStep.cs) updates the current project to use an updated .NET Core or .NET Standard TFM based on project properties and the user's preference for LTS, Current, or Preview. More details on TFM selection are given below.
  - [`PackageUpdaterStep`](../src/steps/Microsoft.DotNet.UpgradeAssistant.Steps.Packages/PackageUpdaterStep.cs) uses registered [`IDependencyAnalyzer`](../src/common/Microsoft.DotNet.UpgradeAssistant.Abstractions/Dependencies/IDependencyAnalyzer.cs) instances to apply upgrades based on the project's dependencies.
    - DependencyAnalzyers are covered in more depth in [extensibility dependency analyzer docs](extensibility.md#dependency-analyzers).
  - [`PackageUpdaterPreTFMStep`](../src/steps/Microsoft.DotNet.UpgradeAssistant.Steps.Packages/PackageUpdaterPreTFMStep.cs) is just like `PackageUpdaterStep` except that it's run prior to updating the project's TFM, so it will remove transitive dependencies but likely won't change any package versions. This step is meant as a way to clean up package references for users who want to modernize their project files without completely upgrading to .NET Core or .NET 5/6.
  - [`TemplateInserterStep`](../src/steps/Microsoft.DotNet.UpgradeAssistant.Steps.Templates/TemplateInserterStep.cs) adds template files appropriate for the project type to the project being upgraded if they don't exist yet.
  - [`SourceUpdaterStep`](../src/steps/Microsoft.DotNet.UpgradeAssistant.Steps.Source/SourceUpdaterStep.cs) uses Roslyn analyzers and code fix providers to identify code patterns in C# and VB files that will need updated as part of the upgrade process.
  - [`ConfigUpdaterStep`](../src/steps/Microsoft.DotNet.UpgradeAssistant.Steps.Configuration/ConfigUpdaterStep.cs) uses registered `IUpdater<ConfigFile>` instances to apply project-level upgrades based on the contents of web.config and app.config files.
    - [`IUpdater<ConfigFile>`](../src/common/Microsoft.DotNet.UpgradeAssistant.Abstractions/IUpdater.cs) is covered in more depth in [extensibility docs](extensibility.md#updaters).
  - [`RazorUpdaterStep`](../src/extensions/web/Microsoft.DotNet.UpgradeAssistant.Steps.Razor/RazorUpdaterStep.cs) updates Razor views (cshtml files) in the project using registered `IUpdater<RazorCodeDocument>` implementations.
    - [`IUpdater<RazorCodeDocument>`](../src/common/Microsoft.DotNet.UpgradeAssistant.Abstractions/IUpdater.cs) is covered in more depth in [extensibility docs](extensibility.md#updaters).
  - [`NextProjectStep`](../src/steps/Microsoft.DotNet.UpgradeAssistant.Steps.Solution/NextProjectStep.cs) runs after upgrading a specific projects is complete and clears the current project from the upgrade context. This will cause most project-specific steps to reset their state the next time the UpgraderManager checks their applicability, causing them to run again on the next project the user selects.
  - [`FinalizeSolutionStep`](../src/steps/Microsoft.DotNet.UpgradeAssistant.Steps.Solution/FinalizeSolutionStep.cs) is intended to run as the last step of the upgrade process and performs cleanup activities such as deleting the .upgrade-assistant state file.

### Other services

- [`IUpgradeReadyCheck`](../src/common/Microsoft.DotNet.UpgradeAssistant.Abstractions/IUpgradeReadyCheck.cs) implementations are used by the `CurrentProjectSelectionStep` to validate that projects are able to be meaningfully upgraded by Upgrade Assistant and can be successfully loaded. Ready checks can indicate that a project either represents unsupported scenarios (but the tool can run anyhow and will partially work) or that the project cannot be upgraded at all. Current ready check implementations include:
  - [`CanLoadProjectFile`](../src/components/Microsoft.DotNet.UpgradeAssistant/Checks/CanLoadProjectFile.cs) verifies that the project can be successfully loaded (the project file is well-formed and necessary imports are present).
  - [`CentralPackageManagementCheck`](../src/components/Microsoft.DotNet.UpgradeAssistant/Checks/CentralPackageManagementCheck.cs) confirms that central NuGet package version management isn't in use as this is not yet supported by Upgrade Assistant.
  - [`MultiTargetFrameworkCheck`](../src/components/Microsoft.DotNet.UpgradeAssistant/Checks/MultiTargetFrameworkCheck.cs) ensures that SDK-style projects being upgraded have a `<TargetFramework>` property rather than a `<TargetFrameworks>` property as multiple TFMs are not yet supported.
  - [`WcfServerCheck`](../src/components/Microsoft.DotNet.UpgradeAssistant/Checks/WcfServerCheck.cs) checks whether WCF server-side APIs (like ServiceHost) are in use as these project types often cannot be upgraded very well. Because it is possible for Upgrade Assistant to run on such a project (it will just leave the WCF server code unchanged), this check can be bypassed.
  - [`WebFormsCheck`](../src/components/Microsoft.DotNet.UpgradeAssistant/Checks/WebFormsCheck.cs) checks for WebForms usage as WebForms projects cannot be upgraded very usefully by the tool. Because it is possible for Upgrade Assistant to run on a WebForms project (it will just ignore all the WebForms dependencies), this check can be bypassed.
  - [`VisualBasicRazorTemplateCheck`](../src/extensions/vb/Microsoft.DotNet.UpgradeAssistant.Extensions.VisualBasic/VisualBasicRazorTemplateCheck.cs) checks for the existence of vbhtml views in the project since vbhtml is unsupported on .NET Core. Because projects with these dependencies can still be upgraded (the VB Razor views will just be ignored), this check can be bypassed.
- [Roslyn analyzers and code fix providers](../src/extensions/default/analyzers) are used by the `SourceUpdaterStep` and `RazorUpdaterStep` to identify patterns in source code that users will need to update and, if a code fix provider is available, will automatically make those fixes. Extensions can add their own analyzers and code fix providers to extend this functionality.

### TFM selection

- [`ITargetFrameworkSelector`](../src/common/Microsoft.DotNet.UpgradeAssistant.Abstractions/ITargetFrameworkSelector.cs) implementations determine the recommended TFM for given input projects. The default implementation ([`TargetFrameworkSelector`](../src/components/Microsoft.DotNet.UpgradeAssistant/TargetFramework/TargetFrameworkSelector.cs)) chooses .NET Standard 2.0 whenever possible (since it is the most portable target for modern development) and uses a series of [`ITargetFrameworkSelectorFilter`](../src/common/Microsoft.DotNet.UpgradeAssistant.Abstractions/ITargetFrameworkSelectorFilter.cs) implementations to update this baseline until the correct TFM is found. These filters (which can be easily added to by registering additional filters in DI) look at factors like project output type, dependencies' TFMs, and project properties. `ITargetFrameworkSelectorFilter` implementations in the default extensions include:
  - [`ExecutableTargetFrameworkSelectorFilter`](../src/components/Microsoft.DotNet.UpgradeAssistant/TargetFramework/ExecutableTargetFrameworkSelectorFilter.cs) ensures that projects built as exes build using an executable .NET target (.NET Core 3.1, .NET 5, or .NET 6, depending on the user's preferences) rather than .NET Standard.
  - [`WebProjectTargetFrameworkSelectorFilter`](../src/components/Microsoft.DotNet.UpgradeAssistant/TargetFramework/WebProjectTargetFrameworkSelectorFilter.cs) ensures that web projects build against executable .NET targets since ASP.NET and ASP.NET Core apps should target .NET Core 3.1, .NET 5, or .NET 6 after upgrade rather than .NET Standard.
  - [`DependencyMinimumTargetFrameworkSelectorFilter`](../src/components/Microsoft.DotNet.UpgradeAssistant/TargetFramework/DependencyMinimumTargetFrameworkSelectorFilter.cs) ensures the TFM a project is upgraded to is not less than the TFMs of that project's dependencies. So, for example, a project that has dependencies built against .NET 5 must target at least .NET 5 (not 3.1 or netstandard2.0) itself.
  - [`WindowsSdkTargetFrameworkSelectorFilter`](../src/components/Microsoft.DotNet.UpgradeAssistant/TargetFramework/WindowsSdkTargetFrameworkSelectorFilter.cs) ensures that projects with Windows-specific dependencies (WinForms, WPF, etc.) use a TFM with the -windows target.
  -[`MyTypeTargetFrameworkSelectorFilter`](../src/extensions/vb/Microsoft.DotNet.UpgradeAssistant.Extensions.VisualBasic/MyTypeTargetFrameworkSelectorFilter.cs) ensures that VB projects using the MyType node upgrade to at least net5.0-windows.

### Extensibility

Upgrade Assistant uses an extension model that makes it possible for add-ons to modify the behavior of existing steps and services or add their own. This is done by allowing extensions to both register services in the tool's dependency injection container and by allowing extension options to be registered which can configure services while also allowing other extensions to modify the options. Upgrade Assistant's extension architecture is covered in detail in dedicated [extensibility documentation](extensibility.md).

### Diagram

The architectural components described above are shown in this architecture diagram of Upgrade Assistant's logical components:

![Component architecture](images/component-architecture.png)

### Project architecture validation

The architecture of project is enforced with validation diagrams and will be run automatically at build. See the `eng/DependencyValidation` project to update or adjust this diagram. The following project architecture is currently enforced:

![Dependency diagram](images/dependency-validation.png)

To disable validation for a specific project, set `ValidateLayerDiagram=false`. This is done currently for tests as they are not in the diagram.

Steps to add new project for validation :

- Add the new project as a reference to the DependencyValidation Project
- Open Class View Window from Architecture Menu
- Open the UpgradeAssistant.layerdiagram
- Drag and Drop the new project from Class View to the correct area of the Dependency Diagram.

>Do not ignore the Missing Workloads Installation popup. If you did, check that your VS install has the .NET desktop development workload and the optional Architecture and Analysis Tools component. Look at the documentation for [Dependency Diagrams](https://docs.microsoft.com/visualstudio/modeling/layer-diagrams-guidelines?view=vs-2019#what-is-a-dependency-diagram).
