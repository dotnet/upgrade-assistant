# DevOps

If your contribution requires specific version or installation of particular tooling that is not part of the current pipeline, you can add steps to the build pipeline. To add scripts that run after dotnet tool is installed and before the project is built, use the [restore-toolset.ps1](..\eng\restore-toolset.ps1).


### .NET MAUI Extension

.NET MAUI extension requires the maui workloads for the tests to run. The required workloads are installed via the  [restore-toolset.ps1](..\eng\restore-toolset.ps1) script using ` dotnet workload install maui` command.

Any failures in the IntegrationTest cases for .NET MAUI could possibly have been caused because of the workloads not being installed correctly. Best way to debug the failure, is to check the Build logs to confirm the workloads were installed successfuly. To troubleshoot the workload install command itself, follow the most up to date guidance in the [.NET MAUI installation guide](https://docs.microsoft.com/en-us/dotnet/maui/get-started/installation).