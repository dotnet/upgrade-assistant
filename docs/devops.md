# DevOps

If your contribution requires specific version or installation of particular tooling that is not part of the current pipeline, you can add steps to the build pipeline. To add scripts that run after dotnet tool is installed and before the project is built, use the [restore-toolset.ps1](..\eng\restore-toolset.ps1).