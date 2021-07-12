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
upgrade-assistant extension list
```

This command will list current extensions applied to a project.

```
upgrade-assistant extension add --name [name] [--version [version]] [--source [source]]
```

This command will allow users to add extensions to a project.

```
upgrade-assistant extension remove --name [name]
```

This command will remove an installed extension from a current workspace.

```
upgrade-assistant extension update [--name [name]] [--version [version]]
```

This command will update all extensions, or a specific one if the name is given.

```
upgrade-assistant extension feed list [--source [source]]
```

This command will allow someone to list extensions available on a feed

### Extension Author Commands

```
upgrade-assistant extension feed publish --path [path] --source [source]
```

This command will allow extension authors to publish an extension.

```
upgrade-assistant extension feed remove --name [name] --source [source]
```

This command will allow extension authors to remove a published extension.

```
upgrade-assistant extension feed clean --source [source]
```

This command will allow extension authors to automatically clean up a feed (i.e. if a file has been removed from the storage, this will notify the author as well as remove it from the feed metadata).

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
            "MD5": "...",
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

Problems with this approach includes:

- Would require a redesign of how extensions are packaged
- May be against some ToS of NuGet.org
