# Roslyn Best Practices

Concepts referred to in this document which are not assumed to be familiar for all readers are explained in the following table.

| Name    | Description |
|---------|-------------|
| Analyzer    | **Analyzers** are pieces of code that inspect your C# or Visual Basic code for style, quality, maintainability, and other issues. An **analyzer** contains code that recognizes violations of rules that you define. |
| Code Fixer | A **code fixer** contains the code that fixes the violation by modifying source code. **Code fixes** in Visual Studio appear as "light bulb" suggestions. |
| Language agnostic | We use this phrase to describe when the **analyzer** or **code fixer** is able to operate on both C# and Visual Basic.|

<br />

We use [Roslyn Analyzers](https://docs.microsoft.com/en-us/visualstudio/code-quality/roslyn-analyzers-overview) to highlight areas of code that will need to be refactored. By default, we aim to pair these Analyzers with Code Fixers to automate as much of the upgrade workflow as possible.

Example of changes that we can perform include:
* Mapping from one type to another one common example is [System.Web.Http.ApiController](https://docs.microsoft.com/en-us/previous-versions/aspnet/hh834453(v=vs.118)) should become [Microsoft.AspNetCore.Mvc.ControllerBase](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.controllerbase)
* Extracting HttpContext via method dependency injection
* Replacing methods that are not available on .NET latest such as [BinaryFormatter.UnsafeDeserialize](https://docs.microsoft.com/en-us/dotnet/api/system.runtime.serialization.formatters.binary.binaryformatter.unsafedeserialize)

The following guidelines are a list of best practices that we use to guide the development of Analyzers and Code Fixers to support our goals.

### Our Goals

1. Write language agnostic analyzers with as much code reuse as possible.
2. Analyzers should be as performant as possible.
3. Samples included follow best practices as setup by [Roslyn Analyzers](https://github.com/dotnet/roslyn-analyzers).
4. Use abstractions, and clean code principles, to write code that everyone can read regardless of their Roslyn experience to promote community contributions and reduce code maintenance.

## Best Practices for Roslyn Analyzers and Code Fixes

### 1. Use the *Microsoft.CodeAnalysis.Testing* framework

Separation of analyzer and code fix tests increases complexity and code duplication and tends to decrease the overall confidence in the test suite. If you're testing entire files at a time, you will either be quickly overwhelmed by the number of files per test scenario or be tempted to put multiple test scenarios into a single file which shifts from unit to integration testing. The *Microsoft.CodeAnalysis.Testing* framework addresses these concerns.

**Do**
* Read the testing overview: [Microsoft.CodeAnalysis.Testing](https://github.com/dotnet/roslyn-sdk/blob/main/src/Microsoft.CodeAnalysis.Testing/README.md)

### 2. Avoid member variables and state management
Expect your analyzer to be invoked repeatedly and asynchronously. Design your analyzer so that execution can start processing a 2nd call before processing finishes for the 1st call. 

**Do**
* Pass information between methods via method arguments.

**Do not**
* Do not use member variables to store instance data.

### 3. Use abstractions and focus on the intent of your Analyzer rather than Roslyn
Roslyn is a rich framework of information that describes every detail of code in every file of every project. The concepts can become overwhelming. Use abstractions to develop class names and methods that sharpen the focus on what the analyzer does by hiding how it achieves the goal.

**Do**
* Use extension methods, and wrapper objects to describe "what" the code does instead of "how" the code behaves.

## Best Practices for Roslyn Analyzers

### 1. "Bail out" Quickly
Roslyn sees class files as rich trees of information. There will likely be millions of syntax nodes to evaluate across a large solution. You should limit the performance impact of running the analyzer by reducing the number of operations performed.

**Do**
* Use the [Syntax Visualizer](https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/syntax-visualizer) to build syntax. Where possible use specific syntax options. As an example, if you are building an analyzer that will examine class inheritance then you should use `SyntaxKind.BaseList`, which will occur much less often than the `SyntaxKind.IdentifierName`.
* Use information already available from `SyntaxNodeAnalysisContext` to quickly filter relevant information. As an example, if you build an analyzer that looks for a method then you would be evaluating `MemberAccessExpressionSyntax` nodes. In this scenario, you should also check the parent of the node to figure out if this Syntax is a method or a property.

**Do Not**
* Do not put string compare operations above other conditional checks that can be performed. String comparison is slower than checking for a type or examining an enumeration value.


### 2. Beware of String manipulations
Build your analyzer with the expectation that it will be invoked a million times. Look for, and replace, strings to prevent excessive garbage collection due to frequent executions of your analyzer.

**Do**
* Use string constants when evaluating string conditionals.

**Do Not**
* Do not use string interpolation, concatenation, or format strings which construct new objects each time the analyzer is run.

### 3. Enable your Analyzer to run concurrently
Enable concurrent execution of your analyzers and prevent them from running in auto generated code.

**Do**
* Use the following in your diagnostic analyzer class.
```cs
    public override void Initialize(AnalysisContext context)
    {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            ...
    }
```

### 4. Directly share node locations with the code fixer
If you track multiple syntax nodes in your Analyzer then you can make it easier to write your Code Fixer by using the method overload of `Diagnostic.Create` that supports [additionalLocations](https://docs.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.diagnostic.create#Microsoft_CodeAnalysis_Diagnostic_Create_Microsoft_CodeAnalysis_DiagnosticDescriptor_Microsoft_CodeAnalysis_Location_System_Collections_Generic_IEnumerable_Microsoft_CodeAnalysis_Location__System_Collections_Immutable_ImmutableDictionary_System_String_System_String__System_Object___)

### 5. Beware of Trivia
When searching for patterns of code it is common to perform string comparisons. These approaches can work successfully but you should beware that trivia, the blank space we use to make code more readable, can vary from team to team. Not everyone puts a space after an assignment operator and if there are two spaces, or a tab, after the assignment operator that code is still valid.

**Should Not**
* Try to avoid `ToFullString` and solutions that require awareness of trivia.

### 6. Report the diagnostic only on parts of code that would be changed by a code fixer.
 This is a better communication to the end-user when running in Visual Studio and it makes it easier to write the Code Fixer.

### 7. Do not use async methods in the synchronous context of an Analyzer. 
If the only methods available are async, you should find another way to implement your analyzer to prevent runtime issues.

**Do not**
* Use `.Result` or `Wait()` in a Roslyn Analyzer. Forcing asynchronous code to behave synchronously can result in timing issues that are hard to debug and thread starvation.

## Best Practices for Roslyn Code Fixers

### 1. Code fixers handle well-known scenarios
You may need to do a few final checks before registering your code fixer by calling `RegisterCodeFix` but you should lean into doing as many checks as possible in the analyzer.

You can also accelerate your code fixer productivity by using the additionalLocations overload when reporting a diagnostic so that you can find multiple interesting locations in a document when working from a single diagnostic.

### 2. Use the `SyntaxGenerator` to create language agnostic code fixers
In many scenarios, your code fixer can apply to Csharp and VB. The SyntaxGenerator  is a language agnostic factory for creating syntax nodes.
 
The trees generated by this API will try to respect user preferences when possible. For example, generating MemberAccessExpression(SyntaxNode, String) will be done in a way such that `this.` or `Me.` will be simplified according to user preference if any `ReduceAsync(Document, OptionSet, CancellationToken)` overload is called.

### 3. Beware of Trivia
When replacing a SyntaxNode consider whether the node you're replacing had trivia. Trivia, the blank space we use to make code more readable, can vary from team to team. Not everyone puts a space after an assignment operator and if there are two spaces, or a tab, after the assignment operator then the code fixer should respect the file's original formatting.

**Do**
* Use the `WithLeadingTrivia` and `WithTrailingTrivia` extension methods to preserve trivia when creating new nodes.

### 4. Apply Async programming best practices
Code fixers are asynchronous. Follow best practice guidance for asynchronous programming to prevent race conditions and performance issues.

Learn more about Async best practices by reading [Async/Await - Best Practices in Asynchronous Programming](https://docs.microsoft.com/en-us/archive/msdn-magazine/2013/march/async-await-best-practices-in-asynchronous-programming) by Stephen Cleary

**Do**
* Leverage the CancellationToken to interrupt processing when cancellation has been requested.

**Do not**
* Do not force async code to run synchronously. Avoid calling `.Result` and prefer to use await as needed when working in the code fixer construct.

### 5. When using CodeAction.Create prefer Document over Solution
Minimize the impact of running a code fixer by using the smallest possible scope. 

When writing your CodeFixer you need to create a CodeAction to resolve the diagnostic. If your code fixer only changes a single document, then your CodeAction should return a document.

**Should**
* Prefer `Task<Document>` when possible and use `Task<Solution>` as needed when returning a result from a Code Fixer.

### 6. Use SolutionEditor and DocEditor when you need to batch changes
When you need to make many changes, you will want to batch those changes with the SolutionEditor, or DocEditor. It can be helpful to think about these objects as you would think about using StringBuilder when concatenating many strings. These objects enable you to create a cumulative list of changes that can reduce GC pressure created by changing immutable SyntaxTrees.

If you’re not batching changes, then most code changes can be carried out by working directly with the document’s syntax root and the SyntaxGenerator.


### 7. Enable support for Fix All
Don’t force users to fix instances of a diagnostic one-by-one. A FixAll occurrences code fix means: I have a code fix 'C', that fixes a specific instance of diagnostic 'D' in my source, and I want to apply this fix to all instances of 'D' across a broader scope, such as a document or a project or the entire solution.

Your code fixer should override `GetFixAllProvider` to return a non-null instance of [FixAll Provider](https://github.com/dotnet/roslyn/blob/main/docs/analyzers/FixAllProvider.md).

### 8. Treat the Code Fixer as a single unit of work
Code fixers are applied as a one-step change and their order is not guaranteed. If the code you’re adding requires a namespace import, then your code fixer should perform that change if necessary. Do not assume that another code fixer will handle adding the correct namespace.

## Considerations specific to upgrade-assistant

There are many stopping points on the journey from .NET Framework to .NET latest and even more scenarios specific to our customers. Because of this, `upgrade-assistant` does not always assume that the code can be compiled. This section highlights some considerations specific to the "work in progress" state that all projects will pass through as they are upgraded.

### 1. Do not assume the code will compile
`upgrade-assistant` will make sweeping changes across the solution. These sweeping changes often result in manual changes that must be made after running upgrade assistant before the code will compile.

As an example, `upgrade-assistant` will upgrade NuGet packages across major versions. `upgrade-assistant` does not evaluate if the newer package contains breaking changes.

### 2. Symbols may not be resolvable
`upgrade-assistant` will add, upgrade, and remove package references during the upgrade process. If your analyzer is looking for a specific symbol then you may need to [AddMetadataReference](https://docs.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.project.addmetadatareference) to ensure the symbol is available.

### 3. Add reference assemblies instead of the assembly implementation
Reference assemblies are a special type of assembly that have only the smallest amount of metadata needed to represent the library's public API surface.

If you need to [AddMetadataReference](https://docs.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.project.addmetadatareference) then use a reference assembly when possible.

Examples include:
* [Microsoft .NET Framework Reference Assemblies .NET 4.8](https://www.nuget.org/packages/Microsoft.NETFramework.ReferenceAssemblies.net48/)
* [Microsoft .NET Framework Reference Assemblies .NET 4.7.2](https://www.nuget.org/packages/Microsoft.NETFramework.ReferenceAssemblies.net472/)

## Tips and more resources

### 1. Use the Syntax Visualizer
https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/syntax-visualizer

The Syntax Visualizer is a tool window that helps you inspect and explore syntax trees. It's an essential tool to understand the models for code you want to analyze. It's also a debugging aid when you develop your own applications using the .NET Compiler Platform (“Roslyn”) SDK.

### 2. Look at examples
* [Roslyn-analyzers](https://github.com/dotnet/roslyn-analyzers)
* [Roslynator](https://github.com/JosefPihrt/Roslynator)

### 3. Video Training Courses
* [Youtube: Learn Roslyn Now](https://www.youtube.com/watch?v=wXXHd8gYqVg) by Josh Varty