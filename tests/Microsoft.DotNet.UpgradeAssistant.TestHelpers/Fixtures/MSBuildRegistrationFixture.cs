// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Autofac.Extras.Moq;
using Microsoft.DotNet.UpgradeAssistant.MSBuild;

namespace Microsoft.DotNet.UpgradeAssistant.Fixtures
{
    public class MSBuildRegistrationFixture
    {
        public MSBuildRegistrationFixture()
        {
            // Register MSBuild
            using var mock = AutoMock.GetLoose();
            var msBuildRegistrar = mock.Create<MSBuildRegistrationStartup>();

            msBuildRegistrar.RegisterMSBuildInstance();
        }
    }
}
