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

        public static UserManager<string, string> MembershipTest(Foo.Membership membership)
        {
            MembershipUser currentUser = Web.Security.Membership.GetUser(User.Identity.Name, true /* userIsOnline */);
            System.Web.Security.FormsAuthentication.SetAuthCookie(currentUser.UserName);
            Microsoft.AspNet.Identity.Owin.SignInManager<string, string> = new SignInManager<string, string>();
        }

        public HttpRequest MemberTest()
        {
            var x = HttpRequest.RawUrl;
            System.Web.HttpRequest request1;
            Microsoft.AspNetCore.Http.HttpRequest request2;
            HttpRequest request3;
            x = request1.RawUrl;
            x = request2.RawUrl;
            x = request3.RawUrl;

            var y = System.Web.HttpRequest.RawUrl;
        }
    }

    public class SignInManager<T, U> { }
}

namespace Foo
{
    public interface IHttpModule { }
}

namespace ServiceModel
{
    public class ServiceHostBase { }
}
