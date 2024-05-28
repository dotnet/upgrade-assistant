# Upgrade Assistant

This repository is now home to 3rd-party extensions to the [Visual Studio Upgrade Assistant](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.upgradeassistant)
and its [CLI](https://www.nuget.org/packages/upgrade-assistant#versions-body-tab) alternative.

## Status

| |Build (Debug)|Build (Release)|
|---|:--:|:--:|
| ci |[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/upgrade-assistant/dotnet.upgrade-assistant?branchName=main&jobName=Windows_NT&configuration=Windows_NT%20Debug)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=953&branchName=main)|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/upgrade-assistant/dotnet.upgrade-assistant?branchName=main&jobName=Windows_NT&configuration=Windows_NT%20Release)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=953&branchName=main)|
| official | [![Build Status](https://dev.azure.com/dnceng/internal/_apis/build/status/dotnet/upgrade-assistant/dotnet-upgrade-assistant?branchName=main&stageName=Build&jobName=Windows_NT&configuration=Windows_NT%20Debug)](https://dev.azure.com/dnceng/internal/_build/latest?definitionId=949&branchName=main)|[![Build Status](https://dev.azure.com/dnceng/internal/_apis/build/status/dotnet/upgrade-assistant/dotnet-upgrade-assistant?branchName=main&stageName=Build&jobName=Windows_NT&configuration=Windows_NT%20Release)](https://dev.azure.com/dnceng/internal/_build/latest?definitionId=949&branchName=main)|

## Overview

This project aims to bring extensibility to the dotnet Upgrade Assistant tool. One of the extensibility points are mappings such as [Package Maps](docs/Extensibility.PackageMaps.md)
and [API Maps](docs/Extensibility.APIMaps.md), allowing third-party vendors to provide the necessary information needed to upgrade projects from old versions of .NET and/or old
vendor APIs to a newer version.

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

### Extensibility

#### Mappings Extension

##### Directory Structure

In the [src/Microsoft.UpgradeAssistant.Mappings/mappings](src/Microsoft.UpgradeAssistant.Mappings/mappings) directory, each vendor *SHOULD* create their own subdirectory.
Each vendor *MAY* decide to subdivide their vendor-specific subdirectory into further subdirectories based on product names or any other criteria that makes sense for
their needs.

For example, the [sample mappings directory](samples/mappings) contains a subdirectory called *Microsoft* that is further subdivided by subdirectories including
*AzureFunctions*, *Common*, *Maui*, *Web*, *WinUI* and *Windows.Forms*.

Nested in these subdirectories are 3 types of files: *metadata.json*, *packagemap.json*, and *apimap.json*.

##### metadata.json

Whether you want to provide mappings for NuGet package upgrades or API changes, you'll want to have a **metadata.json** file.

The *metadata.json* file should look something like this:

```json
{
  "traits": "<some traits go here>",
  "order": 1000
}
```

The *traits* and *order* metadata in *metadata.json* files are automatically inherited by any *packagemap.json* and *apimap.json* files that exist
in the same directory or any subdirectory (regardless of subdirectiory depth).

###### The "traits" property

The *traits* property is a string that defines in what circumstances the Upgrade Assistant should apply the package mappings and/or API mappings.

For example, if a vendor wants to upgrade NuGet package references for a **Xamarin.Forms** project, they might use a *traits* string of `Xamarin`
or `(Xamarin | Maui)`.

More information about traits can be found [here](docs/Traits.md).

###### The "order" property

The *order* property is an integer value that is used for the purposes of sorting the order in which package mapping changes and API mapping changes are applied.

We recommend a starting value of *1000* for vendor-specific mappings.

##### Package Maps

Package Maps define how a NuGet package reference in a project can be upgraded to reference either an alternative NuGet package or a newer version of the same
NuGet package.

Below is an example *packagemap.json* file with comments that explain some of the different possibilities:

```json
{
  "packages": [
    {
      "name": "Vendor.ProductName",
      "frameworks": {
        ".NETCoreApp,Version=v5.0": [
          // An empty array will remove the package from projects with the specified target framework.
        ],
        ".NETCoreApp,Version=v6.0": [
          {
            // An empty package definition will upgrade the package to the latest available version of the same package name.
            // When the "name" property is null or unspecified, it will automatically default to the original package name.
            // When the "version" is null or unspecified, it will default to the latest version available.
            // Thus, if neither are specified, it will default to the latest version of the original package.
          }
        ],
        ".NETCoreApp,Version=v7.0": [
          {
            // Specifying a "name" and "version" will upgrade the package to an exact package.
            "name": "Vendor.NewProductName",
            "version": "7.0.11"
          }
        ],
        ".NETCoreApp,Version=v8.0": [
          {
            // Specifying a "name" and a wildcard "version" will upgrade the package to the latest version that matches the wildcard version.
            "name": "Vendor.ProductName.Abstractions",
            "version": "8.*",

            // Specifying "prerelease": true will tell the UpgradeAssistant that it can match against prerelease versions.
            "prerelease": true
          },
          {
            "name": "Vendor.ProductName.Core",
            "version": "8.*",
            "prerelease": true
          }
        ]
      }
    }
  ]
}
```

It is also worth noting that in the above example, when the Upgrade Assistant is upgrading a project to .NET 8.0, it will upgrade references to `Vendor.ProductName` to
`Vendor.ProductName.Abstractions` *and* `Vendor.ProductName.Core`. This is useful in scenarios where a package has been broken into multiple packages.

##### API Maps

API Maps define how the Upgrade Assistant should transform namespaces, type names, method names and property names in user-code when upgrading a project.

An *apimap.json* file consists of a dictionary of API mappings. An example *apimap.json* file with only 1 mapping might look like this:

```json
{
  "Windows.UI.WindowManagement.AppWindow.TryCreateAsync": {
    "value": "Microsoft.UI.Windowing.AppWindow.Create", // new value to replace old one with, if empty if state is not Replaced
    "kind": "method", // method|property|namespace|type
    "state": "Replaced", // Replaced|Removed|NotImplemented
    "isStatic": true,
    "needsManualUpgrade": false, // if true, only comment is added, no other code modifications happening
    "documentationUrl": "some url", // link to documentation URL,
    "needsTodoInComment": true, // if true TODO is added to the comment if comment is being added
    "isAsync": false,
    "messageId": "resource id", // in case custom comment needs to be added, this resource id will be looked up in the ResourceManager,
    "MessageParams": [ "", "" ] // parameters to be passed into string format for custom message
  }
}
```

The above example would transform all occurrences of `Windows.UI.WindowManagement.AppWindow.TryCreateAsync()` in user-code with `Microsoft.UI.Windowing.AppWindow.Create`.

Since the Upgrade Assistant uses Roslyn to parse and manipulate user-code, even user-code that calls `AppWindow.TryCreate()` will be upgraded.

## Engage, Contribute and Give Feedback

Some of the best ways to contribute are to use the tool to upgrade your apps to the latest version of .NET (STS, LTS, or preview), file issues for feature-requests or bugs, join in design conversations, and make pull-requests. 

Check out the [contributing](/CONTRIBUTING.md) page for more details on the best places to log issues, start discussions, PR process etc.

Happy upgrading to the latest version of .NET (STS, LTS, or preview)!
