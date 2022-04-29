# Migrating from Xamarin.Forms to .NET MAUI

upgrade-assistant include preview support for Xamarin.Forms to .NET MAUI migration. Please note this tool only has preview support and more features coming soon. This tool helps with the migration progress but you will still require to make manual changes to the solution to make it run successfully with .NET MAUI.

## Supported Project types and minimum requirements

- Xamarin.Forms app using Xamarin.Forms 5.0 or higher
  - ✅ iOS project
  - ✅ Android project
  - ❌ UWP project 
  - ❌ iOS App extension and other extension types for example Share Extension project

- Complete environment setup for .NET MAUI including all necessary workloads installed. Confirm that your environment can build and run a blank .NET MAUI template app. Be sure to follow all steps mentioned in the [Getting Started](https://github.com/dotnet/maui/wiki#getting-started). 
- Use version control, create a backup of current code before starting the upgrade process

## Implementation

Currently supported steps:

- csproj updates
    - update csproj files to follow net6.0 format and add/remove relevant project properties.
    - add relevant `TargetFrameworks` to csproj file

- nuget updates
    - Removes known packages not supported by .NET MAUI
    - Adds known ports of libraries for .NET MAUI

- source code updates
    - Switches `using Xamarin.Forms` with `using Microsoft.Maui`
    - Removes `using Xamarin.Essentials`

## Migration Steps

After running the upgrade-assistant tool, you still have to make manual changes to your projects. Complete the rest of the migration process manually, by following: 

- [Manual Migration Guide](https://github.com/dotnet/maui/wiki/Migrating-from-Xamarin.Forms-(Preview))
- [.NET MAUI Documentation](https://docs.microsoft.com/en-us/dotnet/maui/)
- [Samples](https://github.com/dotnet/maui/wiki/Migrating-from-Xamarin.Forms-(Preview)#samples)

### Issues

For .NET MAUI specific issues, or issues/feedback around migrating Xamarin.Forms apps to .NET MAUI, lease File Issues [here](https://github.com/maddymontaquila/maui-migration-samples/issues/new?assignees=&labels=&template=trial-migration-template.md&title=[MIGRATION]+Your+migration+name+here). 

For upgrade-assistant tool related issues, please follow [here](https://github.com/dotnet/upgrade-assistant#engage-contribute-and-give-feedback).

For .NET MAUI related issues, please check the [.NET MAUI Github Repo](https://github.com/dotnet/maui).
