# Contributing

One of the easiest ways for you to contribute is to participate in discussions on GitHub issues. You can also contribute by submitting pull requests with code changes.

## General feedback and discussions?

Start a discussion on the [repository issue tracker](https://github.com/dotnet/upgrade-assistant/issues).

## Bugs and feature requests?

For non-security related bugs, please [log a new issue](https://github.com/dotnet/upgrade-assistant/issues) or simply click [this link](https://github.com/dotnet/upgrade-assistant/issues/new?assignees=&labels=bug&template=20_bug_report.md).

## Get and build source code

In order to get the source, clone the repo with submodules:

```bash
git clone https://github.com/dotnet/upgrade-assistant
git submodule update --init --recursive
```

Once complete, launch `UpgradeAssistant.Extensions.sln` from the upgrade-assistant directory.

## How to submit a PR

We are always happy to see PRs from community members both for bug fixes as well as new features.

To help you be successful we've put together a few simple rules to follow when you prepare to contribute to our codebase:

### Before submitting the pull request

Before submitting a pull request, make sure that it checks the following requirements:

- You find an existing issue with the "help-wanted" label or discuss with the team to agree on adding a new issue with that label
- You post a high-level description of how it will be implemented, and receive a positive acknowledgement from the team before getting too committed to the approach or investing too much effort implementing it
- You add test coverage following existing patterns within the codebase
- Your code matches the existing syntax conventions within the codebase
- Your PR is small, focused, and avoids making unrelated changes
  
If your pull request contains any of the below, it's less likely to be merged:

- Changes that break existing functionality.
- Changes that are only wanted by one person/company. Changes need to benefit a large enough portion of upgrade-assistant users.
- Changes that add entirely new feature areas without prior agreement
- Changes that are mostly about refactoring existing code or code style
- Very large PRs that would take hours to review (remember, we're trying to help lots of people at once). For larger work areas, please discuss with us to find ways of breaking it down into smaller, incremental pieces that can go into separate PRs.

### Submitting a pull request

You will need to sign a [Contributor License Agreement](https://cla.dotnetfoundation.org/) when submitting your pull request. To complete the Contributor License Agreement (CLA), you will need to follow the instructions provided by the CLA bot when you send the pull request. This needs to only be done once for any .NET Foundation OSS project.

If you don't know what a pull request is read this article: https://help.github.com/articles/using-pull-requests. Make sure the repository can build and all tests pass. Familiarize yourself with the project workflow and our coding conventions. For general coding guidelines, see [here](https://github.com/dotnet/aspnetcore/wiki/Engineering-guidelines#coding-guidelines).

### During pull request review

A core contributor will review your pull request and provide feedback. To ensure that there is not a large backlog of inactive PRs, the pull request will be marked as stale after two weeks of no activity. After another four days, it will be closed.

## Resources to help you get started

Here are some resources to help you get started on how to contribute code or new content.

- Look at the [Contributor documentation](/README.md) to get started on building the source code on your own.
- The **UpgradeAssistant.Mappings.Tests** project will validate that the syntax of packagemap and apimap json files is correct.

### Feedback

Your pull request will now go through extensive checks by the subject matter experts on our team. Please be patient while upgrade-assistant team gets through it. Update your pull request according to feedback until it is approved by one of the upgrade-assistant team members. Once the PR is approved, one of the upgrade-assistant team members will merge your PR into the repo.

## Code of conduct

See [CODE-OF-CONDUCT.md](./CODE-OF-CONDUCT.md)
