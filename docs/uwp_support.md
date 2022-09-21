# Migrating from UWP to Windows App SDK

We are currently building out support for upgrade-assistant to handle UWP to Windows App SDK migrations. Please note this tool only has preview support and more features coming soon. This tool helps with the migration progress but you will still require to make manual changes to the solution to make it run successfully with Windows App SDK.

## Supported functionality and minimum requirements

  - ✅ Upgrade csproj to the newer SDK style format
  - ✅ Update project TFM to net6.0
  - ✅ Try to add supported nuget packages and remove unsupported ones
  - ✅ Update the package.appxmanifest file to the newer style
  - ✅ Try to detect and fix APIs that have changed, and marks APIs that are no longer supported, with //TODO code comments.
  - ✅ ApplicationView APIs are supported.
  - ✅ AppWindow related APIs are supported (though it tries to generate a warning where possible and deliberately breaks your code so it doesn't compile until you manually fix things).
  - ❌ Custom Views not supported (For example a CustomDialog that extends MessageDialog and calls an api incorrectly, it will not be warned about or fixed).
  - ✅ WinRT Components are supported.
  - ❌ Multi window apps might not convert correctly.
  - ✅ Support for apps that follow a nonstandard file structure (Such as App.xaml, App.xaml.cs missing from the root folder).

- Complete environment setup for Windows App SDK including all necessary workloads installed. Confirm that your environment can build and run a blank Windows App SDK template app. Be sure to follow all steps mentioned in the [Getting Started](https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/).
- We recommend using version control to create a backup of your current codebase before starting the upgrade process.

## Migration Steps

After running the upgrade-assistant tool, you still have to make manual changes to your projects. Complete the rest of the migration process manually, by following: 

- [Manual Migration Guide](https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/migrate-to-windows-app-sdk-ovw)
- [Windows App SDK Documentation](https://docs.microsoft.com/en-us/windows/apps/develop/)
- [Case Study](https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/case-study-1)

### Issues

"If you attempt an upgrade, please file an issue [here](https://github.com/dotnet/upgrade-assistant/issues) with `area:UWP` tag to let us know how it went and what issues you ran into!

For upgrade-assistant tool related issues, please follow [here](https://github.com/dotnet/upgrade-assistant#engage-contribute-and-give-feedback).

For Windows App SDK related issues, please check the [Windows App SDK Github Repo](https://github.com/microsoft/WindowsAppSDK).
