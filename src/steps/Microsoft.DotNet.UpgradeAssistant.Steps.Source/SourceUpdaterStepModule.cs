using System.Linq;
using Autofac;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CSharp.Analyzers;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CSharp.CodeFixes;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Source
{
    public class SourceUpdaterStepModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<SourceUpdaterStep>().As<MigrationStep>().InstancePerLifetimeScope();

            // Although this only registers analyzers and code fix providers from the default extension,
            // other extensions can register additional analyzers and code fix providers in their own modules.
            builder.RegisterAssemblyTypes(typeof(UsingSystemWebAnalyzer).Assembly)
                .Where(t => t.IsPublic && t.CustomAttributes.Any(a => a.AttributeType.Equals(typeof(DiagnosticAnalyzerAttribute))))
                .As<DiagnosticAnalyzer>();
            builder.RegisterAssemblyTypes(typeof(UsingSystemWebCodeFixer).Assembly)
                .Where(t => t.IsPublic && t.CustomAttributes.Any(a => a.AttributeType.Equals(typeof(ExportCodeFixProviderAttribute))))
                .As<CodeFixProvider>();
        }
    }
}
