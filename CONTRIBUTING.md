# Contributing

One of the easiest ways for you to contribute is to participate in discussions on GitHub issues. You can also contribute by submitting pull requests with code changes.

## General feedback and discussions?
Start a discussion on the [repository issue tracker](https://github.com/dotnet/upgrade-assistant/issues).

## Bugs and feature requests?
For non-security related bugs, please [log a new issue](https://github.com/dotnet/upgrade-assistant/issues) or simply click [this link](https://github.com/dotnet/upgrade-assistant/issues/new?assignees=&labels=bug&template=20_bug_report.md).

## How to submit a PR

We are always happy to see PRs from community members both for bug fixes as well as new features.
To help you be successful we've put together a few simple rules to follow when you prepare to contribute to our codebase:

**Finding an issue to work on**

  We've created a separate bucket of issues, which would be great candidates for community members to contribute to. We mark these issues with the `help wanted` label. You can find all these issues [here](https://github.com/dotnet/upgrade-assistant/issues?q=is%3Aissue+is%3Aopen+label%3A%22help+wanted%22).
  
Within that set, we have additionally marked issues which are good candidates for first-time contributors. Those do not require too much familiarity with the codebase and are more novice-friendly. Those are marked with `good first issue` label. The full list of such issues can be found [here](https://github.com/dotnet/upgrade-assistant/issues?q=is%3Aissue+is%3Aopen+label%3A%22good+first+issue%22).
  
If there is some other area not included here where you want to contribute to, first open an issue to describe the problem you are trying to solve and state that you're willing to contribute a fix for it. We will then discuss in that issue, if this is an issue we would like addressed and the best approach to solve it.
  
**Before writing code**

  This can save you a lot of time. We've seen PRs where customers solve an issue in a way which either wouldn't fit into upgrade-assistant because of how it's designed or would change upgrade-assistant in a way which is not something we'd like to do. To avoid these situations, we encourage customers to discuss the preferred design with the team first. To do so, file a new `design proposal` issue, link to the issue you'd like to address and provide detailed information about how you'd like to solve a specific problem. We triage issues periodically and it will not take long for a team member to engage with you on that proposal.
  When you get an agreement from our team members that the design proposal you have is solid, then go ahead and prepare the PR.
  To file a design proposal, look for the relevant issue in the `New issue` page or simply click [this link](https://github.com/dotnet/upgrade-assistant/issues/new?assignees=&labels=design-proposal&template=10_design_proposal.md):
  ![image](https://user-images.githubusercontent.com/34246760/107969904-41b9ae80-6f65-11eb-8b84-d15e7d94753b.png)
  
**Before submitting the pull request**

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

**During pull request review**
A core contributor will review your pull request and provide feedback. To ensure that there is not a large backlog of inactive PRs, the pull request will be marked as stale after two weeks of no activity. After another four days, it will be closed.

## Architecture

Before contributing to Upgrade Assistant, you should be familiar with the logical components that comprise the tool and architecture validation processes, as described in the [Upgrade Assistant architecture documentation](docs/architecture.md).

## Resources to help you get started

Here are some resources to help you get started on how to contribute code or new content.

* Look at the [Contributor documentation](/README.md) to get started on building the source code on your own.
* ["Help wanted" issues](https://github.com/dotnet/upgrade-assistant/labels/help%20wanted) - these issues are up for grabs. Comment on an issue if you want to create a fix.
* ["Good first issue" issues](https://github.com/dotnet/upgrade-assistant/labels/good%20first%20issue) - we think these are a good for newcomers.
* [Best Practices for Roslyn Analyzers and Code Fixers](./docs/roslyn_best_practices.md) - our goal is to build analyzers that are performant and available for both C# and Visual Basic by default.

### Identifying the scale

If you would like to contribute to upgrade-assistant, first identify the scale of what you would like to contribute. If it is small (grammar/spelling or a bug fix) feel free to start working on a fix. If you are submitting a feature or substantial code contribution, please discuss it with the team and ensure it follows the product roadmap. You might also read these two blogs posts on contributing code: [Open Source Contribution Etiquette](http://tirania.org/blog/archive/2010/Dec-31.html) by Miguel de Icaza and [Don't "Push" Your Pull Requests](https://www.igvita.com/2011/12/19/dont-push-your-pull-requests/) by Ilya Grigorik. All code submissions will be rigorously reviewed and tested further by the upgrade-assistant team, and only those that meet an extremely high bar for both quality and design/roadmap appropriateness will be merged into the source.

### Submitting a pull request

You will need to sign a [Contributor License Agreement](https://cla.dotnetfoundation.org/) when submitting your pull request. To complete the Contributor License Agreement (CLA), you will need to follow the instructions provided by the CLA bot when you send the pull request. This needs to only be done once for any .NET Foundation OSS project.

If you don't know what a pull request is read this article: https://help.github.com/articles/using-pull-requests. Make sure the repository can build and all tests pass. Familiarize yourself with the project workflow and our coding conventions. For general coding guidelines, see [here](https://github.com/dotnet/aspnetcore/wiki/Engineering-guidelines#coding-guidelines).

### Tests

[Tests](/tests) in upgrade-assistant follow the following pattern:

- Testing Framework used is [XUnit](https://docs.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-dotnet-test).
- Mocking Framework used is [Moq](https://github.com/Moq/moq4) (with [AutoMock](https://autofaccn.readthedocs.io/en/latest/integration/moq.html)).
- Data generation Framework used is [AutoFixture](https://github.com/AutoFixture/AutoFixture/wiki/Cheat-Sheet).

Tests need to be provided for every bug/feature(except docs or samples) that is completed.

### Feedback

Your pull request will now go through extensive checks by the subject matter experts on our team. Please be patient while upgrade-assistant team gets through it. Update your pull request according to feedback until it is approved by one of the upgrade-assistant team members. Once the PR is approved, one of the upgrade-assistant team members will merge your PR into the repo.

### Dev Environment FAQ
The tool may produce long file paths during build, in order to not run into PathTooLongException either change the LongPathsEnabled setting under registry or build upgrade-assistant from a folder location with a shorter path.

## Code of conduct
See [CODE-OF-CONDUCT.md](./CODE-OF-CONDUCT.md)
