# Changelog
All notable changes to the .NET Upgrade Assistant will be documented in this file. Version numbers below will follow (best effort) the corresponding NuGet package versions here: https://www.nuget.org/packages/upgrade-assistant/

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/).

## Current
### Added
- Added support for WPF and Unit Test projects written with Visual Basic [#403](https://github.com/dotnet/upgrade-assistant/pull/403)

### Fixed
- Fixed issue surfacing from floating package references. [#371](https://github.com/dotnet/upgrade-assistant/pull/371)
- Microsoft.AspNetCore.Mvc.NewtonsoftJson package should no longer be added to .NET Framework projects [#290](https://github.com/dotnet/upgrade-assistant/pull/390)
- Fixed issue to ignore disabled NuGet sources. [#396](https://github.com/dotnet/upgrade-assistant/issues/396)

## Version 0.2.217201 - 2021-03-23  ([Link](https://www.nuget.org/packages/upgrade-assistant/0.2.217201))
### Added
- Include try-convert version in upgrade-assistant package #358
- Add check to ensure project files can be successfully loaded prior to upgrading them. #346
- Add a command to select a different current project #343
- Add a step to update package dependecies before TFM change #342
- Add a source updater to update BinaryFormatter.UnsafeDeserialize calls to BinaryFormatter.Deserialize in appropriate cases. #339
- Be sure to include log exception information if upgrade steps fail #335
- Update package updater to upgrade System.Configuration and System.Data.Entity references. #324
- Include try-convert as part of build #322
- More extensibility samples and docs #305
- Add support to load VB project files #299
- Add support for PCL #294
- Add trouble-shooting tips for installing with invalid/auth NuGet sources #291
- Add a sample demonstrating how to make a custom upgrade step #283
- Add option to provide entry-point and make non-interactive mode visible #282
- Add a way to verify upgrade readiness #279
- Allow controlling file/console logging independently #278
- Add SetPropertyValue to IProjectFile and MSBuildProject.File #274

### Changed
- Update web references in libraries #354
- Don't save 'upgrade complete' in state file #352
- Set an error code if the app terminates unexpectedly #348
- Allow sub-steps to be conditional on project components #317
- Sets the default serializer to Newtonsoft for Web Apps to improve backward compatibility #306
- Tool now exits with error if run from non-Windows machine #281
- Change to only target windows for now #264
- Package Microsoft.DotNet.UpgradeAssistant.Extensions as a NuGet package #261

### Fixed
- Don't prompt the user to select a current project if all projects related to the entry point are upgraded. #351
- Fix bugs when upgrading just one project in a larger solution and updated ready checks to apply per-project instead of solution-wide. #314

### Removed
- Remove MSBuild from being included in projects #325


## Version 0.2.212405 - Preview 1 - 2021-02-24  ([Link](https://www.nuget.org/packages/upgrade-assistant/0.2.212405))

The Preview 1 release, was the first public release that supports upgrading ASP.NET MVC, Windows Forms, WPF, Console, and Class Libraries .NET Framework applications to .NET 5.
