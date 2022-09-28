# Extension Management

Upgrade Assistant's extensibility provides the ability for users to extend it beyond its original design. As we've worked with developers making extensions, it's clear that there are some issues with discoverability and maintainability of custom extensions. Some of the major pain points:

- **Version management:** Need to be able to specify which extension version a project should use
- **Distribution** Need to be able easily get a specific version (or update existing ones) of an extension
- **Consistency between runs** Should be able to register an extension so subsequent runs on any machine automatically use it
- **Dependency among extensions [Not In Scope]** - Some extensions provide configuration or additional data for a different extension and currently cannot define that relationship

Throughout this document, the term `workspace` will be used to refer to a solution or project that is being updated along with the state file Upgrade Assistant generates.

## User Experience

Extensions will now be something that are managed at a workspace level. This will allow users to have a consistent experience as well as be able to manage their extensions.

Below are the commands to manage extensions:

```
upgrade-assistant extensions list
```

This command will list current extensions applied to a project.

```
upgrade-assistant extensions restore
```

This command will restore current extensions applied to a project.

> NOTE: This will hopefully be removed in the near future

```
upgrade-assistant extensions add --name [name] [--version [version]] [--source [source]]
```

This command will allow users to add extensions to a project.

```
upgrade-assistant extensions remove --name [name]
```

This command will remove an installed extension from a current workspace.

```
upgrade-assistant extensions update [--name [name]] [--version [version]]
```

This command will update all extensions, or a specific one if the name is given.

```
upgrade-assistant extensions create [path]
```

This command will create a valid NuGet package given an extension path.

### Future Experiences
There are scenarios that make sense to have a global set up (i.e. a developer working on multiple projects). Future work can potentially implement a `--global` option for extensions that would update a local list that would apply everywhere.

### Behavior changes to UA
- At startup, the tool will need to restore any extensions that are registered and ensure those get added.
- `--extension` option will continue to exist, but should be updated to recommend persisting the extension if working on a team

## Workspace state file

Each workspace already contains a state file that tracks progress of the tool, but this is transient information. A new file will be added to a user's repo that is expected to be persisted that will store information about their workspace which will then ensure developers working on a project will get similar results.

The file will be json, similar to the state file. The extension section will look similar to this:

```
{
    ...
    "Extensions":[
        {
            "Name": "...",
            "Version": "...",
            "Source": "...",
        }
    ]
}
```

This will allow `upgrade-assistant` to restore extensions from any source and ensure they are the extension that was originally installed via hash matching.

For now, the name of the file will be `upgrade-assistant.json`. We should update guidance that `.upgrade-assistant` can be ignored, but `upgrade-assistant.json` is used to ensure consistency among developers.

## Sources

We will use NuGet as a source feed and align extension packages with with the `.nupkg` format. This would provide a number of things out of the box, such as:

- Rights management for feed access
- Versioning support
- Validation support
- Signing validation if required

## Local cache

Extensions will be downloaded to `%LOCALAPPDATA%\Microsoft\DotNet Upgrade Assisistant\[source]\[name]\[version]` where:

- `[source]` is a SHA256 hash of the source path
- `[name]` is the name of the extension
- `[version]` is the version of the extension
