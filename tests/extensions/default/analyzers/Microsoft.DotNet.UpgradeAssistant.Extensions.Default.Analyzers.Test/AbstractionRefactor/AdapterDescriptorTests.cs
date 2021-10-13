// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Test.AbstractionRefactor
{
    public class AdapterDescriptorTests : AdapterTestBase
    {
        public Task<ImmutableArray<MetadataReference>> References { get; } = ReferenceAssemblies.NetStandard.NetStandard20.ResolveAsync(LanguageNames.CSharp, default);

        [Fact]
        public async Task SingleProject()
        {
            // Arrange
            using var ws = new AdhocWorkspace();
            var (descriptor, project) = await AddDescriptorProject(ws);

            // Act
            var compilation = (await project.GetCompilationAsync())!;
            var context = AdapterContext.Create().FromCompilation(compilation);

            // Assert
            Assert.Empty(compilation.GetParseDiagnostics());
            Assert.Empty(compilation.GetDiagnostics());

            var expectedOriginal = compilation.GetTypeByMetadataName(descriptor.FullOriginal);
            var expectedDestination = compilation.GetTypeByMetadataName(descriptor.FullDestination);

            Assert.NotNull(expectedOriginal);
            Assert.NotNull(expectedDestination);

            Assert.Collection(context.Descriptors, d =>
            {
                Assert.Equal(expectedOriginal, d.Original);
                Assert.Equal(expectedDestination, d.Destination);
            });
        }

        private async Task<Project> AddProjectAsync(AdhocWorkspace ws, string name)
        {
            var referenceAssemblies = await References;
            var project = ws.AddProject(name, LanguageNames.CSharp);

            return project
                .WithMetadataReferences(referenceAssemblies)
                .WithCompilationOptions(project.CompilationOptions!.WithOutputKind(OutputKind.DynamicallyLinkedLibrary));
        }

        private async Task<(AdapterDescriptorFactory Factory, Project Project)> AddDescriptorProject(AdhocWorkspace ws)
        {
            var descriptor = new AdapterDescriptorFactory("RefactorTest", "ISome", "SomeClass");
            var testFile = @"
namespace RefactorTest
{
    public class SomeClass
    {
    }

    public interface ISome
    {
    }
}";

            var project = await AddProjectAsync(ws, "test");

            project = project
                .AddDocument("classes.cs", testFile)
                .Project
                .AddDocument("attributes.cs", Attribute)
                .Project
                .AddDocument("descriptor.cs", descriptor.CreateAttributeString(LanguageNames.CSharp))
                .Project;

            Assert.True(ws.TryApplyChanges(project.Solution));

            return (descriptor, project);
        }
    }
}
