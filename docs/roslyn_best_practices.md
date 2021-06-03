# Roslyn Best Practices

We use [Roslyn Analyzers](https://docs.microsoft.com/en-us/visualstudio/code-quality/roslyn-analyzers-overview) highlight areas of code that will need to be refactored. By default our goal is to pair these Analyzers with Code Fixers to automate as much of the upgrade workflow as possible.

We support the following types of fixes:
* Mapping from one type to another one common example is [System.Web.Http.ApiController](https://docs.microsoft.com/en-us/previous-versions/aspnet/hh834453(v=vs.118)) should become [Microsoft.AspNetCore.Mvc.ControllerBase](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.controllerbase))
* Extracting HttpContext via method dependency injection
* Replacing methods that are not available on .NET latest such as [BinaryFormatter.UnsafeDeserialize](https://docs.microsoft.com/en-us/dotnet/api/system.runtime.serialization.formatters.binary.binaryformatter.unsafedeserialize)

The following guidelines are a list of best practices that we we use to guide the development of Analyzers and Code Fixers to support our goals.

### Our Goals

1. Write language agnostic analyzers with as much code reuse as possible
2. Analyzers should be as performant as possible
3. Samples included follow best practices as established by [Roslyn Analyzers](https://github.com/dotnet/roslyn-analyzers)
4. Use abstractions, and clean code principles, to write code that everyone can read regardless of their Roslyn experience to promote community contributions and reduce code maintenance

## Roslyn Analyzer Best Practices

### 1. Foo
### 2. Foo
### 3. Foo
### 4. Foo

## Roslyn Code Fixer Best Practices

### 1. Foo
### 2. Foo
### 3. Foo
### 4. Foo

## Considerations specific to upgrade-assistant

There are many stopping points on the journey from .NET Framework to .NET latest and even more scenarios specific to our customers. Because of this, `upgrade-assistant` does not always assume that the code can be compiled. This section highlights some considerations specific to the "work in progress" state that all projects will pass through as they are upgraded.