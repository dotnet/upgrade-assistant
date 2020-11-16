# ASP.NET Migrator

## Overview

This project enables automation of common tasks related to migrating ASP.NET MVC and WebAPI projects to ASP.NET Core. Note that this is not a complete migration tool and work will be required after using the tooling on an ASP.NET project to complete migration.

## Terminology

| Name    | Description |
|---------|-------------|
| Step    | A step can define commands that can perform actions on the project. Each step implements `MigrationStep`.|
| Command | A command is an action that can be invoked by a user to perform some action. |

## Usage

TBD