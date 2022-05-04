# Migrating from Xamarin.Forms to .NET MAUI

We are currently building out support for upgrade-assistant to handle Xamarin.Forms to .NET MAUI migrations. Please note this tool only has preview support and more features coming soon. This tool helps with the migration progress but you will still require to make manual changes to the solution to make it run successfully with .NET MAUI.

## Supported Project types and minimum requirements

- Xamarin.Forms app using Xamarin.Forms 5.0 or higher
  - ✅ iOS project
  - ✅ Android project
  - ❌ UWP project
  - ❌ iOS App extension and other extension types for example Share Extension project
  - ❌ watchOS project
  - ❌ tvOS project

- Complete environment setup for .NET MAUI including all necessary workloads installed. Confirm that your environment can build and run a blank .NET MAUI template app. Be sure to follow all steps mentioned in the [Getting Started](https://github.com/dotnet/maui/wiki#getting-started).
- We recommend using version control to create a backup of your current codebase before starting the upgrade process.

## Implementation

Currently supported steps:

- csproj updates
    - update csproj files to follow net6.0 format and add/remove relevant project properties.
    - add relevant `TargetFrameworks` to csproj file

- nuget updates
    - Removes known packages not supported by .NET MAUI
    - Adds known ports of libraries for .NET MAUI
    - Replaces Xamarin.Community toolkit with .NET MAUI Community toolkit nuget
    - Replaces SkiaSharp Xamarin nugets with SkiaSharp .NET MAUI compatible nugets

- source code updates
    - Switches `using Xamarin.Forms` with `using Microsoft.Maui`
    - Removes `using Xamarin.Essentials`

## Migration Steps

After running the upgrade-assistant tool, you still have to make manual changes to your projects. Complete the rest of the migration process manually, by following: 

- [Manual Migration Guide](https://github.com/dotnet/maui/wiki/Migrating-from-Xamarin.Forms-(Preview))
- [.NET MAUI Documentation](https://docs.microsoft.com/en-us/dotnet/maui/)
- [Samples](https://github.com/dotnet/maui/wiki/Migrating-from-Xamarin.Forms-(Preview)#samples)

### Issues

"If you attempt an upgrade, please file an issue [here](https://github.com/maddymontaquila/maui-migration-samples/issues/new?assignees=&labels=&template=trial-migration-template.md&title=[MIGRATION]+Your+migration+name+here) to let us know how it went and what issues you ran into!

For upgrade-assistant tool related issues, please follow [here](https://github.com/dotnet/upgrade-assistant#engage-contribute-and-give-feedback).

For .NET MAUI related issues, please check the [.NET MAUI Github Repo](https://github.com/dotnet/maui).
