This folder is for Roslyn analyzers and code fixers that will be used to
update source. 

The ASP.NET Migration tool will probe assemblies in this directory at
runtime and run any analyzers found (types marked with the 
[Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer]) to identify potential
migration issues. Similarly, code fix providers (types marked with
Microsoft.CodeAnalysis.CodeFixes.ExportCodeFixProviderAttribute) will be
loaded dynamically from assemblies in this directory and used, if applicable,
to fix the diagnostics.