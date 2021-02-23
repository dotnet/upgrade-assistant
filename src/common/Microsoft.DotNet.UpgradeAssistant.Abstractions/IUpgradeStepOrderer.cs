// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public interface IUpgradeStepOrderer
    {
        bool TryAddStep(UpgradeStep newStep);

        bool TryRemoveStep(string stepId);

        IEnumerable<UpgradeStep> UpgradeSteps { get; }
    }
}
