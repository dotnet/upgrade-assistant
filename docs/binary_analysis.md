# Binary Analysis in Upgrade Assistant

The Binary Analysis feature (`analyzebinaries`) of Upgrade Assistant allows you to determine if a binary - for which you may/may not have the source code - is able to be upgraded to a target framework and platform combination of your choosing.

## Support

Binary Analysis currently supports .NET 6.0 as the target framework and Windows and/or Linux as the target platform.

> Note: Binary Analysis is currently in Preview. To enable it, you must add `ANALYZE_BINARIES` to your `UA_FEATURES` environment variable. Read more about feature enablement [here](https://github.com/dotnet/upgrade-assistant#experimental-features).

## Usage

### Command Line

```
analyzebinaries <files-or-directories> [options]

Arguments:
  <files-or-directories>  The binary file(s) or directory(ies) to analyze for compatibility

Options:
  -pre, --allow-prerelease                        Allow pre-release packages to be considered as an option for support of the target framework/platform combination
  -obs, --obsoletion                              Include information about obsoleted APIs
  -p, --platform <Linux|Windows>                  The OS platform(s) to check availability for (e.g. linux windows) [default: Linux]
  -t, --target-tfm-support <Current|LTS|Preview>  Select if you would like the Long Term Support (LTS), Current, or Preview TFM. See
                                                  https://dotnet.microsoft.com/platform/support/policy/dotnet-core for details on what these mean.
  -v, --verbose                                   Enable verbose diagnostics
  -f, --format <format>                           Specify format of analyze result. If not provided, a sarif file will be produced. Available default values:
                                                  "sarif", "html"
  -?, -h, --help                                  Show help and usage information
```

#### Example Usage

**Single File**

To analyze a single binary file, simply pass the path to the command

`analyzebinaries c:\bin\myfile.dll`

**Multiple Files**

Multiple files can be passed to the command as separate arguments:

`analyzebinaries c:\bin\file1.dll c:\bin\file2.dll "c:\Program Files\bin\myfile.exe"`

**Directories**

To analyze all files in a given directory, pass the directory as the argument to the command:

`analyzebinaries c:\bin`

**Mixed mode**

Additionally, Binary Analysis supports the ability to specify both directories and files as its input:

`analyzebinaries c:\bin "c:\Program Files\bin\myfile.exe"`

## Interpreting the results

Upon completion, Binary Analysis will highlight zero or more warnings in your binaries based on the target you've chosen. These warnings include:

Rule ID | Name | Notes | Action
-|-|-|-
`UA9000` | InvalidAssembly | The assembly that was analyzed was not a valid .NET Assembly. | None
`UA9010` | RuleName | The cited API is Unavailable in target framework. | You'll need to update your code to use a different API to make it compatible with the target framework.
`UA9011` | ApiAvailableViaExternalPackage | API is available via an external Package (NuGet) | You can bring the binary forward but you'll need to include an external package from NuGet for it to function on the target framework.
`UA9020` | ApiObsoleted | The API is available in the target framework but has been marked as `[Obsolete]` | The obsolete warning in the target framework may/may not break your build and you'll want to follow the instructions presented by it to modify your code accordingly.
`UA9030` | PlatformNotSupported | API is unsupported in target platform. | The platform you're looking to go to (e.g., Windows, Linux) doens't support the API so you'll need to find its equivalent, or remove/`#IFDEF` its usage.
