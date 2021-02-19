// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public interface IMigrationStepOrderer
    {
        bool TryAddStep(MigrationStep newStep);

        bool TryRemoveStep(string stepId);

        IEnumerable<MigrationStep> MigrationSteps { get; }
    }
}
