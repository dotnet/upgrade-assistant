# Changelog
All notable changes to the .NET Upgrade Assistant will be documented in this file. Version numbers below will follow (best effort) the corresponding NuGet package versions here: https://www.nuget.org/packages/upgrade-assistant/

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/).

## Current

### Fixed
- Updated `HttpContext.Current` code fix to use an internal `HttpContextHelper` that will work in multi-project solutions [#599](https://github.com/dotnet/upgrade-assistant/pull/599).

## Version 0.2.227701 - 2021-05-27 ([Link](https://www.nuget.org/packages/upgrade-assistant/0.2.227701))

### Added
- Added an analyzer and code fix provider to upgrade System.Web.Mvc.Controller to Microsoft.AspNetCore.Mvc.Controller [#534](https://github.com/dotnet/upgrade-assistant/pull/534)
- Added additional code fixer for `HttpContext.Current` that will replace calls with method injection [#536](https://github.com/dotnet/upgrade-assistant/pull/536)
- Added a Razor upgrade sub-step to replace @helper functions in Razor views with local methods [#559](https://github.com/dotnet/upgrade-assistant/pull/559)
- Analyzers that recommend replacing one type with another are now combined into a single analyzer (`TypeUpgradeAnalyzer`) with behavior that can be customized via AdditionalTexts containing old -> new type mappings [#540](https://github.com/dotnet/upgrade-assistant/pull/540)
- BinaryFormatterUnsafeDeserializer now works with Visual Basic [#544](https://github.com/dotnet/upgrade-assistant/pull/544)

### Fixed
- VB projects that have a MyType property that requires Windows will now default to net5.0-windows [#529](https://github.com/dotnet/upgrade-assistant/pull/529)
- Restores are now more likely to be performed if needed so errors about finding targets won't be surfaced. A clearer message will be surfaced as well if this occurs. [#525](https://github.com/dotnet/upgrade-assistant/pull/525)
- Updated the ApiController upgrade code fix provider to upgrade to Microsoft.AspNetCore.Mvc.ControllerBase instead of Microsoft.AspNetCore.Mvc.Controller [#534](https://github.com/dotnet/upgrade-assistant/pull/534)

### Breaking change
- Upgrade path now uses the command `upgrade`. In order to use the tool to upgrade projects, the command looks like `upgrade-assistant upgrade <Path to csproj or sln to upgrade>` [#541](https://github.com/dotnet/upgrade-assistant/pull/541)

## Version 0.2.226201 - 2021-05-12 ([Link](https://www.nuget.org/packages/upgrade-assistant/0.2.226201))

### Added
- A new command line option (`--target-tfm-support` to select the support model of LTS/Preview/Current that is desired [#469](https://github.com/dotnet/upgrade-assistant/pull/469)

### Fixed
- VB Win Forms projects should keep import for 'System.Windows.Forms' [#474](https://github.com/dotnet/upgrade-assistant/pull/474)

## Version 0.2.222702 - 2021-04-27 ([Link](https://www.nuget.org/packages/upgrade-assistant/0.2.222702))

### Added
- Multiple entrypoints can now be added by using globbing and multiple instances of the `--entry-point` argument [#425](https://github.com/dotnet/upgrade-assistant/pull/425)
- NuGet credential providers will now be used, if present (may require running in interactive mode) [#448](https://github.com/dotnet/upgrade-assistant/pull/448) 
- Source analyzers and code fix providers are now applied to source embedded in Razor documents [#455](https://github.com/dotnet/upgrade-assistant/pull/455)
- Persist Backup path in .upgrade-assistant state file [#447](https://github.com/dotnet/upgrade-assistant/pull/447) Thanks for the PR, [@oteione](https://github.com/oteinone)!

### Fixed
- UpgradeSteps should be filtered based on project components [#255](https://github.com/dotnet/upgrade-assistant/issues/255)
- Do not add _ViewImports.cshtml to VB projects [#378](https://github.com/dotnet/upgrade-assistant/issues/378)

### Breaking change
- The commandline argument `-e` is now shorthand for `--entry-point` rather than `--extension` [#425](https://github.com/dotnet/upgrade-assistant/pull/425) 

## Version 0.2.220602 - 2021-04-06 ([Link](https://www.nuget.org/packages/upgrade-assistant/0.2.220602))
### Added
- Added support for WPF and Unit Test projects written with Visual Basic [#403](https://github.com/dotnet/upgrade-assistant/pull/403)

### Fixed
- Allows projects to be read if they have multiple TFMs. The tool still can't upgrade them, but it won't block upgrading dependent projects [#379](https://github.com/dotnet/upgrade-assistant/issues/379)
- Fixed issue surfacing from floating package references [#371](https://github.com/dotnet/upgrade-assistant/pull/371)
- Microsoft.AspNetCore.Mvc.NewtonsoftJson package should no longer be added to .NET Framework projects [#376](https://github.com/dotnet/upgrade-assistant/issues/376)
- Fixed issue to ignore disabled NuGet sources [#396](https://github.com/dotnet/upgrade-assistant/issues/396)
- Ensured restore is run when required [#402](https://github.com/dotnet/upgrade-assistant/issues/402)

## Version 0.2.217201 - 2021-03-23  ([Link](https://www.nuget.org/packages/upgrade-assistant/0.2.217201))
### Added
- Include try-convert version in upgrade-assistant package [#358](https://github.com/dotnet/upgrade-assistant/issues/358)
- Add check to ensure project files can be successfully loaded prior to upgrading them. [#346](https://github.com/dotnet/upgrade-assistant/issues/346)
- Add a command to select a different current project [#343](https://github.com/dotnet/upgrade-assistant/issues/343)
- Add a step to update package dependecies before TFM change [#342](https://github.com/dotnet/upgrade-assistant/issues/342)
- Add a source updater to update BinaryFormatter.UnsafeDeserialize calls to BinaryFormatter.Deserialize in appropriate cases. [#339](https://github.com/dotnet/upgrade-assistant/issues/339)
- Be sure to include log exception information if upgrade steps fail [#335](https://github.com/dotnet/upgrade-assistant/issues/335)
- Update package updater to upgrade System.Configuration and System.Data.Entity references. [#324](https://github.com/dotnet/upgrade-assistant/issues/324)
- Include try-convert as part of build [#322](https://github.com/dotnet/upgrade-assistant/issues/322)
- More extensibility samples and docs [#305](https://github.com/dotnet/upgrade-assistant/issues/305)
- Add support to load VB project files [#299](https://github.com/dotnet/upgrade-assistant/issues/299)
- Add support for PCL [#294](https://github.com/dotnet/upgrade-assistant/issues/294)
- Add trouble-shooting tips for installing with invalid/auth NuGet sources [#291](https://github.com/dotnet/upgrade-assistant/issues/291)
- Add a sample demonstrating how to make a custom upgrade step [#283](https://github.com/dotnet/upgrade-assistant/issues/283)
- Add option to provide entry-point and make non-interactive mode visible [#282](https://github.com/dotnet/upgrade-assistant/issues/282)
- Add a way to verify upgrade readiness [#279](https://github.com/dotnet/upgrade-assistant/issues/279)
- Allow controlling file/console logging independently [#278](https://github.com/dotnet/upgrade-assistant/issues/278)
- Add SetPropertyValue to IProjectFile and MSBuildProject.File [#274](https://github.com/dotnet/upgrade-assistant/issues/274)

### Changed
- Update web references in libraries [#354](https://github.com/dotnet/upgrade-assistant/issues/354)
- Don't save 'upgrade complete' in state file [#352](https://github.com/dotnet/upgrade-assistant/issues/352)
- Set an error code if the app terminates unexpectedly [#348](https://github.com/dotnet/upgrade-assistant/issues/348)
- Allow sub-steps to be conditional on project components [#317](https://github.com/dotnet/upgrade-assistant/issues/317)
- Sets the default serializer to Newtonsoft for Web Apps to improve backward compatibility [#306](https://github.com/dotnet/upgrade-assistant/issues/306)
- Tool now exits with error if run from non-Windows machine [#281](https://github.com/dotnet/upgrade-assistant/issues/281)
- Change to only target windows for now [#264](https://github.com/dotnet/upgrade-assistant/issues/264)
- Package Microsoft.DotNet.UpgradeAssistant.Extensions as a NuGet package [#261](https://github.com/dotnet/upgrade-assistant/issues/261)

### Fixed
- Don't prompt the user to select a current project if all projects related to the entry point are upgraded. [#351](https://github.com/dotnet/upgrade-assistant/issues/351)
- Fix bugs when upgrading just one project in a larger solution and updated ready checks to apply per-project instead of solution-wide. [#314](https://github.com/dotnet/upgrade-assistant/issues/314)

### Removed
- Remove MSBuild from being included in projects [#325](https://github.com/dotnet/upgrade-assistant/issues/325)


## Version 0.2.212405 - Preview 1 - 2021-02-24  ([Link](https://www.nuget.org/packages/upgrade-assistant/0.2.212405))

The Preview 1 release, was the first public release that supports upgrading ASP.NET MVC, Windows Forms, WPF, Console, and Class Libraries .NET Framework applications to .NET 5.
