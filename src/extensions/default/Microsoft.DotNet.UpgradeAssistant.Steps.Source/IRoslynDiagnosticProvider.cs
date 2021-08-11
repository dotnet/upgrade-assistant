// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Source
{
    public interface IRoslynDiagnosticProvider
    {
        Task<IEnumerable<Diagnostic>> GetDiagnosticsAsync(IProject project, CancellationToken token);

        IEnumerable<CodeFixProvider> GetCodeFixProviders();

        IEnumerable<DiagnosticDescriptor> GetDiagnosticDescriptors(CodeFixProvider provider);
    }
}
