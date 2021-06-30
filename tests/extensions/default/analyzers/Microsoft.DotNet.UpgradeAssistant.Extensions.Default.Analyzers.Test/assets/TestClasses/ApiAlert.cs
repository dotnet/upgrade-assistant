// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Deployment.Internal;
using System.Deployment.Application;
using System.Deployment.Application.Foo;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Test.assets.TestClasses
{
    public class ApiAlertTest: IHttpModule, System.Web.IHttpHandler, Foo.IHttpModule
    {
        System.Deployment.Application.ApplicationDeployment x { get; set; }
        ApplicationDeployment y;

        public static void TestMethod()
        {
            var z = new Application.ApplicationDeployment();
            var z1 = new System.Deployment.Application.ApplicationDeployment();
            System.Deployment.Application.Foo.Bar x = null;
        }

        [ChildActionOnly]
        public static void TypeAlertTest(ServiceHostBase svc)
        {
            System.Web.Mvc.ChildActionOnlyAttribute a = new Mvc.ChildActionOnlyAttribute();
            ServiceHost x = new ServiceModel.ServiceHostBase();
            BundleCollection b = Web.Optimization.BundleCollection.SomeMethod();
        }
    }
}

namespace Foo
{
    public interface IHttpModule { }
}

namespace ServiceModel
{
    public class ServiceHostBase { }
}
