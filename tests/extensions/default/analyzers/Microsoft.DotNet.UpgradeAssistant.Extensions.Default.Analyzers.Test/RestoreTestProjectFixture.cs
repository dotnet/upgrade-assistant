// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.DotNet.UpgradeAssistant.Fixtures;
using Microsoft.DotNet.UpgradeAssistant.MSBuild;
using Moq;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Test
{
    public class RestoreTestProjectFixture : MSBuildRegistrationFixture
    {
        public RestoreTestProjectFixture()
            : base()
        {
            var context = new Mock<IUpgradeContext>();
            context.Setup(c => c.GlobalProperties).Returns(new Dictionary<string, string>());

            foreach (var lang in new[] { Language.CSharp, Language.VisualBasic })
            {
                var projectLanguage = lang.GetFileExtension();
                var path = TestHelper.TestProjectPath.Replace("{lang}", projectLanguage, StringComparison.OrdinalIgnoreCase);
                var fullpath = Path.Combine(AppContext.BaseDirectory, path);
                Mock.Create<DotnetRestorePackageRestorer>().RunRestoreAsync(context.Object, fullpath, default).GetAwaiter().GetResult();
            }
        }
    }
}
