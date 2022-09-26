# Changelog
All notable changes to the .NET Upgrade Assistant will be documented in this file. Version numbers below will follow (best effort) the corresponding NuGet package versions here: https://www.nuget.org/packages/upgrade-assistant/

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/).


## Version 0.4.346202 - 2022-09-20([Link](https://www.nuget.org/packages/upgrade-assistant/0.4.346202))

### Added
- Generation of post upgrade report for UWP -> Windows App SDK migration (https://github.com/dotnet/upgrade-assistant/pull/1292)

## Version 0.4.346201 - 2022-09-12 ([Link](https://www.nuget.org/packages/upgrade-assistant/0.4.346201))

### Added
- WCF -> CoreWCF migration support (https://github.com/dotnet/upgrade-assistant/pull/1237)
- Post-Upgrade report generation (https://github.com/dotnet/upgrade-assistant/pull/1260)

### Fixed
- MAUI bug fixes (https://github.com/dotnet/upgrade-assistant/pull/1238)
- Binary Analysis `-p windows` error (https://github.com/dotnet/upgrade-assistant/pull/1266)
- Improved the Analysis experience (https://github.com/dotnet/upgrade-assistant/pull/1291)

## Version 0.4.336902 - 2022-07-19 ([Link](https://www.nuget.org/packages/upgrade-assistant/0.4.336902))

### Added
- Binary Analysis support (https://github.com/dotnet/upgrade-assistant/pull/1210)

### Changed
- Format flag (`--format`) now available OOB for Analysis output

### Fixed
- `ConfigurationManager` error (#1151) (https://github.com/dotnet/upgrade-assistant/pull/1162)
- Bug preventing extension Samples (and presumably any external extensions) from being properly loaded (https://github.com/dotnet/upgrade-assistant/pull/1204)
- Doc updates around extension development, SARIF viewing

## Version 0.3.330701 - 2022-06-08 ([Link](https://www.nuget.org/packages/upgrade-assistant/0.3.330701))

### Added
- UWP conversion support to Windows App SDK (https://github.com/dotnet/upgrade-assistant/pull/1121)

### Changed
- Updated install command documentation to include nuget source feed URL (https://github.com/dotnet/upgrade-assistant/pull/1138)

## Version 0.3.326103 - 2022-05-13 ([Link](https://www.nuget.org/packages/upgrade-assistant/0.3.326103))

### Fixed
- Fix template file creation when directory does not exist(https://github.com/dotnet/upgrade-assistant/pull/1084)
- Fix for community reported issues to make analysis messages more actionable (https://github.com/dotnet/upgrade-assistant/issues/1048) and (https://github.com/dotnet/upgrade-assistant/issues/1047) (PR - https://github.com/dotnet/upgrade-assistant/pull/1116)

### Changed
- Update WCF documentation to point to CoreWCF as well as gRPC(https://github.com/dotnet/upgrade-assistant/pull/1111)

## Version 0.3.310801 - 2022-02-08 ([Link](https://www.nuget.org/packages/upgrade-assistant/0.3.310801))
### Added
- Add solution wide project file conversion behind feature flag (https://github.com/dotnet/upgrade-assistant/pull/996)
- Move transitive reference analyzer to step after other analyzers (https://github.com/dotnet/upgrade-assistant/pull/1001)
- Add feature flag to enable running on non-Windows machines (https://github.com/dotnet/upgrade-assistant/pull/1022)
- Add command to show feature flags and if they are enabled (https://github.com/dotnet/upgrade-assistant/pull/1004)
- Add command 'analysis list-formats` to see available formats (https://github.com/dotnet/upgrade-assistant/pull/1029)
- Read evaluated packagereference versions to support CPvM (https://github.com/dotnet/upgrade-assistant/pull/1035)

### Fixed
- Register default extensions first (https://github.com/dotnet/upgrade-assistant/issues/989) (PR - https://github.com/dotnet/upgrade-assistant/pull/1020) - Thanks for the PR @jbearfoot

## Version 0.3.261602 - 2021-12-16 ([Link](https://www.nuget.org/packages/upgrade-assistant/0.3.261602))

### Added
- Added support for analysis report to be generated in HTML format(https://github.com/dotnet/upgrade-assistant/pull/966)

## Version 0.3.256001 - 2021-11-17 ([Link](https://www.nuget.org/packages/upgrade-assistant/0.3.256001))

### Added
- Pushed extensions for loose assembly lookup and Maui (preview) conversions to NuGet.org

### Changed
- Updated SARIF output for analyze to newer version (https://github.com/dotnet/upgrade-assistant/pull/927)

## Version 0.3.255803 - 2021-11-09 ([Link](https://www.nuget.org/packages/upgrade-assistant/0.3.255803))
### Added
- In lieu of .NET 6 GA, Current, LTS and Preview now point to .NET 6 in upgrade-assistant (https://github.com/dotnet/upgrade-assistant/pull/907)

### Fixed
- Razor source updater bug fixes (#919)[https://github.com/dotnet/upgrade-assistant/pull/919]. Issues addressed
  - (https://github.com/dotnet/upgrade-assistant/issues/856)
  - (https://github.com/dotnet/upgrade-assistant/issues/914)
  - (https://github.com/dotnet/upgrade-assistant/issues/915)

## Version 0.3.252501 - 2021-11-01 ([Link](https://www.nuget.org/packages/upgrade-assistant/0.3.252501))
### Added
- Extensions can now be downloaded and installed into an upgrade context via a NuGet feed (see [docs](docs/design/Extension_Management.md) for details) (#873)[https://github.com/dotnet/upgrade-assistant/pull/855]
- Added extension for identifying loose assemblies with data to identify loose assemblies from NuGet.org (see [docs](docs/design/Loose_binary_identification.md) for details) (https://github.com/dotnet/upgrade-assistant/pull/855)
- Source updater for handling HighDpiMode setting in Winforms (https://github.com/dotnet/upgrade-assistant/pull/877)
- More verbose logging for Diagnostic Analysis in Analyze Command (https://github.com/dotnet/upgrade-assistant/pull/877)

### Removed
- Removed option to run `try-convert` as an exe and now will always be run in-process [#870](https://github.com/dotnet/upgrade-assistant/pull/870)

## Version 0.3.246501 - 2021-09-15 ([Link](https://www.nuget.org/packages/upgrade-assistant/0.3.246501))
### Fixed
- Fixed issue with Package Analyzer to not enumerate the same collection that is being modified. [#836](https://github.com/dotnet/upgrade-assistant/issues/836)

## Version 0.3.242703 - 2021-08-31 ([Link](https://www.nuget.org/packages/upgrade-assistant/0.3.242703))
### Added
- Alert users about the deprecated controls in Windows Forms [#800](https://github.com/dotnet/upgrade-assistant/pull/800)
- Added a command line option to specifiy a path to msbuild via `--msbuild-path` ([#802](https://github.com/dotnet/upgrade-assistant/pull/802))
- Support for running on .NET 6 Preview 7 ([#832](https://github.com/dotnet/upgrade-assistant/pull/832))

### Changed
- Removed a check that wouldn't include preview .NET SDK instances ([#802](https://github.com/dotnet/upgrade-assistant/pull/802))
- Log file outputs structured logging rather than just text ([#822](https://github.com/dotnet/upgrade-assistant/pull/822))
- Fixed an issue where running against VS 2017 wouldn't resolve targets in installed workloads ([#832](https://github.com/dotnet/upgrade-assistant/pull/832))
- Do not clear console in between runs so log files aren't lost ([#826](https://github.com/dotnet/upgrade-assistant/pull/826))
- `try-convert` is now hosted in process. If there are any issues with the hosted version, you may revert to old behavior using the feature flag `UA_FEATURES=TRY_CONVERT_EXE` ([#826](https://github.com/dotnet/upgrade-assistant/pull/825))

## Version 0.2.241603 - 2021-08-16 ([Link](https://www.nuget.org/packages/upgrade-assistant/0.2.241603))
### Added
- Analyze command now supports flagging of unsupported API usage in project / solution [#764](https://github.com/dotnet/upgrade-assistant/pull/764)
- Added dependency analyzer for System.Windows.Forms.DataVisualization [#792](https://github.com/dotnet/upgrade-assistant/pull/792)
- Added .NET MAUI extension steps to add TFMs for .NET MAUI [#790](https://github.com/dotnet/upgrade-assistant/pull/790)
    - Adds templates files per project
    - Manages project property transforms as per migration requirements
    - Makes C# source code updates for new .NET MAUI APIs. 

## Version 0.2.237901 - 2021-07-30 ([Link](https://www.nuget.org/packages/upgrade-assistant/0.2.237901))

### Changed
- Updated try-convert tool version from `0.7.226301` to `0.9.232202`

### Added
- Added command line argument to select VS version (`--vs-path`) in cases where multiple are installed [#753](https://github.com/dotnet/upgrade-assistant/pull/753)
- Added analyzers for identifying common namespaces, types, and members that require manual fixup and will produce diagnostics with links to relevant docs. The list of APIs identified by the analyzer can be expanded by adding to DefaultApiAlerts.json or by adding a .apitargets file to a project's additional files. [#685](https://github.com/dotnet/upgrade-assistant/pull/685)
- Link to survey [#735](https://github.com/dotnet/upgrade-assistant/pull/735)

## Version 0.2.236301 - 2021-07-15 ([Link](https://www.nuget.org/packages/upgrade-assistant/0.2.236301))

### Added
- `analyze` command to provide pre-upgrade package analysis and produce a sarif log of the results [#24](https://github.com/dotnet/upgrade-assistant/issues/24)
- Validation diagram to help maintain the architectural decisions so far and codify it into the build process. [#696](https://github.com/dotnet/upgrade-assistant/pull/696)

### Fixed
- Fixed regression where `--skip-backup` and `--entrypoint` options were not being passed through [#695](https://github.com/dotnet/upgrade-assistant/pull/695)

## Version 0.2.233001 - 2021-06-30 ([Link](https://www.nuget.org/packages/upgrade-assistant/0.2.233001))

### Added
- New project readiness checks evaluate if the project contains unsupported technologies to increase awareness before users invest significant time trying to upgrade. [#617](https://github.com/dotnet/upgrade-assistant/pull/617)
- Usage telemetry has been added to help guide product development. See [https://aka.ms/upgrade-assistant-telemetry](https://aka.ms/upgrade-assistant-telemetry) for details [#644](https://github.com/dotnet/upgrade-assistant/pull/644).
- Command line option to pass options through in the form of `--option KEY=Value` [#651](https://github.com/dotnet/upgrade-assistant/pull/651)
- Added an analyzer and code fix provider to remove unnecessary attributes and upgrade changed attributes (based on type mappings in registered typemap files) [#641](https://github.com/dotnet/upgrade-assistant/pull/641)
- The `SourceUpdaterStep` and `RazorSourceUpdater` will now alert the user of any diagnostics from registered analyzers that require manual fixups (because no code fix was available). This allows Upgrade Assistant to notify users of code patterns that it can identify as needing updated but is unable to update automatically. [#662](https://github.com/dotnet/upgrade-assistant/pull/662)
- Added RemoveProperty method to enable removal of a property from the project file. [#668](https://github.com/dotnet/upgrade-assistant/pull/668)

### Fixed
- Updated `HttpContext.Current` analyzer to more correctly identify uses of `HttpContext.Current` that need replaced [#628](https://github.com/dotnet/upgrade-assistant/pull/628).
- The Upgrade Assistant analyzer package no longer adds a WebTypeReplacements.typemap file to projects it's added to (more precisely, the file is present and available for analyzers to use but isn't visible in the solution explorer anymore) [#632](https://github.com/dotnet/upgrade-assistant/pull/632).
- Addressed compile time errors that surfaced from Visual Basic Runtime and the My. namespace ([629](https://github.com/dotnet/upgrade-assistant/pull/629))
- Exposed Sdk in IProjectFile to enable development of custom extensions to add/remove Sdk. [#614](https://github.com/dotnet/upgrade-assistant/issues/614)

## Version 0.2.231403 - 2021-06-14 ([Link](https://www.nuget.org/packages/upgrade-assistant/0.2.231403))

### Fixed
- Updated `HttpContext.Current` code fix to use an internal `HttpContextHelper` that will work in multi-project solutions [#599](https://github.com/dotnet/upgrade-assistant/pull/599).
- Fixed a bug that was preventing the Upgrade Assistant analyzer package from being added to upgraded projects [#620](https://github.com/dotnet/upgrade-assistant/pull/620).
- Fixed a bug in `PackageLoader` that was causing many extraneous warning messages when verbose logging was enabled [#619](https://github.com/dotnet/upgrade-assistant/pull/619).
- Fixed a bug in `SourceUpdaterStep` that was leaving an extra .cs file in projects after upgrade which introduced build errors in the project (since the .cs files were already automatically included) [#616](https://github.com/dotnet/upgrade-assistant/pull/616).
- Exposed Imports in IProjectFile to enable development of custom extensions to add/remove imports. [#612](https://github.com/dotnet/upgrade-assistant/issues/612)

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
