# ASP.NET Migrator

## Overview

This project enables automation of common tasks related to migrating ASP.NET MVC and WebAPI projects to ASP.NET Core. Note that this is not a complete migration tool and work will be required after using the tooling on an ASP.NET project to complete migration.

## Terminology

| Name    | Description |
|---------|-------------|
| Step    | A step can define commands that can perform actions on the project. Each step implements `MigrationStep`.|
| Command | A command is an action that can be invoked by a user to perform some action. |

## Usage

In order to install the tool, you can run the following command:

```
dotnet tool install -g try-migrate --add-source https://trymigrate.blob.core.windows.net/feed/index.json
```

If you add the source to [NuGet's configuration](https://docs.microsoft.com/en-us/nuget/consume-packages/configuring-nuget-behavior) you may omit the `--add-source` parameter. Only non-prelease will be installed with this command; any prerelease version must be explicitly opted into by adding `--version [desired-version]` to the command.