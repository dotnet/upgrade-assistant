# Changelog
All notable changes to the .NET Upgrade Assistant will be documented in this file. Version numbers below will follow (best effort) the corresponding NuGet package versions here: https://www.nuget.org/packages/upgrade-assistant/

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/).

## Unreleased
### Added
- Include try-convert version in upgrade-assistant package [(#358)](https://github.com/dotnet/upgrade-assistant/pull/358)
- Add check to ensure project files can be successfully loaded prior to upgrading them. [(#346)](https://github.com/dotnet/upgrade-assistant/pull/346)
- Add a command to select a different current project [(#343)](https://github.com/dotnet/upgrade-assistant/pull/343)
- Add a step to update package dependecies before TFM change [(#342)](https://github.com/dotnet/upgrade-assistant/pull/342)
- Add a source updater to update BinaryFormatter.UnsafeDeserialize calls to BinaryFormatter.Deserialize in appropriate cases. [(#339)](https://github.com/dotnet/upgrade-assistant/pull/339)
- Be sure to include log exception information if upgrade steps fail [(#335)](https://github.com/dotnet/upgrade-assistant/pull/335)
- Update package updater to upgrade System.Configuration and System.Data.Entity references. [(#324)](https://github.com/dotnet/upgrade-assistant/pull/324)
- Include try-convert as part of build [(#322)](https://github.com/dotnet/upgrade-assistant/pull/322)
- Update ready checks to apply per-project instead of solution-wide [(#309)](https://github.com/dotnet/upgrade-assistant/pull/309)
- More extensibility samples and docs [(#305)](https://github.com/dotnet/upgrade-assistant/pull/305)
- Add support to load VB project files [(#299)](https://github.com/dotnet/upgrade-assistant/pull/299)
- Add trouble-shooting tips for installing with invalid/auth NuGet sources [(#291)](https://github.com/dotnet/upgrade-assistant/pull/291)
- Add a sample demonstrating how to make a custom upgrade step [(#283)](https://github.com/dotnet/upgrade-assistant/pull/283)
- Add option to provide entry-point and make non-interactive mode visible [(#282)](https://github.com/dotnet/upgrade-assistant/pull/282)
- Add a way to verify upgrade readiness [(#279)](https://github.com/dotnet/upgrade-assistant/pull/279)
- Allow controlling file/console logging independently [(#278)](https://github.com/dotnet/upgrade-assistant/pull/278)
- Add SetPropertyValue to IProjectFile and MSBuildProject.File [(#274)](https://github.com/dotnet/upgrade-assistant/pull/274)

### Changed
- Update web references in libraries [(#354)](https://github.com/dotnet/upgrade-assistant/pull/354)
- Don't save 'upgrade complete' in state file [(#352)](https://github.com/dotnet/upgrade-assistant/pull/352)
- Set an error code if the app terminates unexpectedly [(#348)](https://github.com/dotnet/upgrade-assistant/pull/348)
- Allow sub-steps to be conditional on project components [(#317)](https://github.com/dotnet/upgrade-assistant/pull/317)
- Sets the default serializer to Newtonsoft for Web Apps to improve backward compatibility [(#306)](https://github.com/dotnet/upgrade-assistant/pull/306)
- Update MSBuildProject to give better error if TFM can't be read [(#281)](https://github.com/dotnet/upgrade-assistant/pull/281)
- Change to only target windows for now [(#264)](https://github.com/dotnet/upgrade-assistant/pull/264)
- Package Microsoft.DotNet.UpgradeAssistant.Extensions as a NuGet package [(#261)](https://github.com/dotnet/upgrade-assistant/pull/261)

### Fixed
- Don't prompt the user to select a current project if all projects related to the entry point are upgraded. [(#351)](https://github.com/dotnet/upgrade-assistant/pull/351)
- Update to 5.0.200 which fixes some extra CA warnings [(#323)](https://github.com/dotnet/upgrade-assistant/pull/323)
- Fix bugs when upgrading just one project in a larger solution and updated ready checks to apply per-project instead of solution-wide. [(#314)](https://github.com/dotnet/upgrade-assistant/pull/314)
- Cleanup for analyzers [(#256)](https://github.com/dotnet/upgrade-assistant/pull/256)

### Removed
- Remove MSBuild from being included in projects [(#325)](https://github.com/dotnet/upgrade-assistant/pull/325)


## Version 0.2.212405 - Preview 1 - 2021-02-24  ([Link](https://www.nuget.org/packages/upgrade-assistant/0.2.212405))

The Preview 1 release, was the first public release that supports upgrading ASP.NET MVC, Windows Forms, WPF, Console, and Class Libraries .NET Framework applications to .NET 5.
