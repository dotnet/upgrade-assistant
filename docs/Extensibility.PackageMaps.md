# Package Map transformations

## Overview

Package Maps define how a NuGet package reference in a project can be upgraded to reference either an alternative NuGet package or a newer version of the same
NuGet package.

## Package map format

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
