// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public class AnalyzeContext
    {
        public AnalyzeContext(IUpgradeContext context)
        {
            UpgradeContext = context;
        }

        public IUpgradeContext UpgradeContext { get; }

        public void RaiseNotification(Notification notification)
        {
            // _list.add(notification);
        }

        // public void RaiseError(Notification notification);
    }

    public class Notification
    {
        public string DocumentationLink { get; set; }
    }
}
