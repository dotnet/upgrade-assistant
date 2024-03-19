# Traits

## What are traits and how they work

When contracts are discovered, we need to have a way to filter out contracts that are applicable to:

- current project kind
- current upgrade kind
- project language
- anything else that comes up in the future.

To generalize this filtering logic, we use `traits` expressions specified in the contract attributes like `SliceNodeTransformer` or `SliceNodeProvider`. This expression has similar syntax as in VS project system's capabilities expressions and platform contracts. However, traits set can contain any trait objects and capabilities are just one example of it.

To produce this set of unique trait objects we use generic contracts `ITraitProvider` which are annotated via `TraitProviderAttribute` and in turn could be filtered per project flavor. We run all trait providers applicable to current project and combine them into set of unique trait objects. Trait object could be a plain `string` (like capability) or an implementation based on `Trait` class.

When trait is a simple string then just basic capabilities like expression operators are applied to it:

- & and operator,
- | or operator,
- ! operator,
- () grouping
- trait characters should be alpha numeric and here is a list of disallowed characters that cannot be used in the trait name "'`:;,+-*/\\!~|&%$@^()={}[]<>? \t\b\n\r
- spaces are ignored

Here is an example of simple expression:

```text
Web & !CPS | (OutputTypeLibrary & CSharp)
```

Which would result in `true` for classic web app or C# class library projects.

When trait object is an implementation of `Trait`, during expression matching we would run `Trait.EvaluateAsync` method where we would pass a `TraitToken` object to check and return `true` or `false`. `TraitToken` object is generated from a special token operator that can also be specified in the trait expression for a contract:

- token is specified in the following format: `{key=value}` where we support following operators between key and a value: =,<,>,<=,>=. Key and value can be any kind of string constructed with allowed characters and a particular `Trait` implementation knows how to interpret them and apply each operator. Key can be a composite object in the form of `KeyName.Property` if we see the dot we see that the name of this `Trait` is `KeyName`.

An example of such `Trait` object is `TargetFramework` trait:

```text
Inplace & !CPS & {TargetFramework.Name=net} & {TargetFramework.Version<5.0} & CSharp
```

When this expression is parsed, we see that there is a `TargetFramework` trait token and try to find a `Trait` object in the set of project traits with the name `TargetFramework`. If found we pass `TraitToken` object having key, operator and value and let this trait evaluate them and return `true` or `false`. Note: for simplicity when dealing with target frameworks we understand short names and full names.

In the example above trait expression would result in true if project is targeting .NET Framework with version less than 5.0 and upgrade operation is running in-place and project is not SDK style.

Note: If a contract does not have traits expression in the attribute, it is applicable to all scenarios.

## Existing traits

So far we have providers for following kind of traits:

- project capabilities coming from msbuild
- project's OutputType property concatenated with its value: `OutputTypeLibrary`, `OutputTypeExe`, `OutputTypeWinExe` etc.
- language: `CSharp`, `VB`
- for web projects we ensure web trait: `Web`
- upgrade kind: `Inplace`, `SideBySide`, `Incremental`
- some properties that we collect during upgrade operation flow, like `NewProject` etc
- target framework composite trait
- for windows projects we add either `WPF` or `WinForms`
- for Xamarin.iOS and Android projects, we have `Xamarin`. Additionally, there are also `IOS` and `Android` traits for the specific platforms.
- for MAUI projects, we have `Maui`
- when running in Visual Studio, we add `VS` trait
