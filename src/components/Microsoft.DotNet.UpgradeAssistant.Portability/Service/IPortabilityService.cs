// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;

namespace Microsoft.DotNet.UpgradeAssistant.Portability.Service
{
    public interface IPortabilityService
    {
        IAsyncEnumerable<ApiInformation> GetApiInformation(IReadOnlyCollection<string> apis, CancellationToken token);
    }
}
