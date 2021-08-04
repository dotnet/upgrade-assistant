// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Source
{
    public interface IDiagnosticAnalysisRunner
    {
        Task<IEnumerable<Diagnostic>> GetDiagnosticsAsync(IProject project, CancellationToken token);
    }
}
