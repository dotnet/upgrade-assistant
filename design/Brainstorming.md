# Design Brainstorming

## Architecture

The obvious part of this tool is the analyzers/code fixes that will flag (and in some cases fix) API usage
that is not compatible with ASP.NET Core.

Less clear is how we migrate project files, web.config, etc.

There will probably need to be a high-level migration function that consumes a csproj and takes the following actions:

1. Creates a new csproj (from a template) in a new location.
    1. The template will include references to analyzers and relevant 3rd-party frameworks.
    1. This could maybe be done with try-convert
1. Copies source and content to the new location?
    1. Should we copy all files or only those used by the csproj?
        1. Only copying those that are used means we would lose items like readmes, licenses, or other docs that might not be included in the project.
        1. But copying all could mean copying bin or obj folders if we're not careful.
    1. Might actually be best to just back stuff up elsewhere and work in-place.
1. Updates the template project based on the csproj, web.config, and other files in the source.
1. Applies code fixes preemptively.
1. Updates appsettings.json with app settings?
1. Logs all changes made.

## User experience thoughts

It may also be good to have an option to update a project in-place, but these will be such breaking changes that a backup will almost certainly be required, so it's ok to force choosing another location for now.

UX will probably eventually be WPF-based, but console is fine for the being. Make sure anything interesting happens in a library so that I can have a variety of front-ends.

## Comparison with general-purpose migration tooling

Migrating ASP.NET apps is different from general migration of .NET Framework projects. This tooling will focus on ASP.NET-specific concerns (using ASP.NET Core APIs and patterns, mostly). General purpose concerns (which might be higher-priority for [a general purpose migration tool][MigrationToolBrainstorm] can be added in the future, but will not be part of initial POCs. Those features include:

* Running the Portability Analyzer and showcasing results in an actionable way.
    * This is lower priority for ASP.NET scenarios because the ASP.NET changes needed will likely be far larger than non-ASP.NET changes.
* Migrating NuGet packages.
    * This would definitely be nice-to-have, but some packages are only used by ASP.NET boilerplate and can go away. It's not a large costs for users to just migrate these by hand in the short-term. Longer-term, this feature will definitely be useful here. It's just not core like it would be in non-web scenarios.
* Guidance on choosing a proper .NET target.
    * This tool is only used for migrating classic ASP.NET MVC and WebAPI apps to ASP.NET Core (targeting .NET Core 5). Questions about which .NET target platform makes sense don't apply here the way they would in a more general purpose migration scenario.

## Similar tools

[try-convert][try-convert] transforms csproj files into new SDK-style projects. This tool will do something similar, but won't be the same as `try-convert` because this tool creates new projects from templates as opposed to just modifying a csproj file in-place. There is enough overlap, though, that this tool is worth looking at to see if we can share any project file processing logic. In fact, with some modifications, try-convert could be used to make some of the needed changes.


[MigrationToolBrainstorm]: https://microsoft.sharepoint.com/:w:/t/DevDivCustomerEngagement/EYfzMzbM0gFPix1duQVAqeABCeevgYcJ410l-oJ9FyBDbQ?CID=D8066BED-6437-4546-8894-2ADB0B7E1DE9&wdLOR=c5BB45068-2652-4F70-B312-C05F984224E2 ".NET Core Migration Tooling Brainstorming"
[try-convert]: https://github.com/dotnet/try-convert
