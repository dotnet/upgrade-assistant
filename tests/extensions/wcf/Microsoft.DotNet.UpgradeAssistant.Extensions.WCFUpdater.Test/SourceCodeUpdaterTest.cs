// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.WCFUpdater.Tests
{
    public class SourceCodeUpdaterTest
    {
        public const string InputWithUsing = @"using Serilog;
using System;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Security;

namespace SampleServer
{
    class Program
    {
        static void Main()
        {
            using (var host = new ServiceHost(typeof(SampleService)))
            {
                host.AddDefaultEndpoints();
                host.Open();
                Console.Writeline(""Service Listening...Press enter to exit."")
                Console.ReadLine();
                host.Exam();
                host.Close();
            }
        }
    }
}";

        public const string Input = @"using Serilog;
using System;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Security;

namespace SampleServer
{
    class Program
    {
        static void Main()
        {
            var host = new ServiceHost(typeof(SampleService));
            host.AddDefaultEndpoints();
            host.Open();
            Console.Writeline(""Service Listening...Press enter to exit."")
            Console.ReadLine();
            host.Exam();
            host.Close();
        }
    }
}";

        public const string Template = @"public static void Main()
{
            var builder = WebApplication.CreateBuilder();

            // Set up port (previously this was done in configuration,
            // but CoreWCF requires it be done in code)
[Port PlaceHolder]

             // Add CoreWCF services to the ASP.NET Core app's DI container
            builder.Services.AddServiceModelServices()
                            .AddServiceModelConfigurationManagerFile(""wcf.config"")

            var app = builder.Build();

            // Configure CoreWCF endpoints in the ASP.NET Core hosts
            app.UseServiceModel(serviceBuilder =>
            {
                serviceBuilder.ConfigureServiceHostBase<ServiceType>(varName =>
                {
                    int UA_placeHolder;
                });
                
                serviceBuilder.AddService<ServiceType>(serviceOptions => {});
            });
            
            app.StartAsync();
            app.StopAsync();
}";

        public const string Added = @"app.StartAsync();
                Console.Writeline(""Service Listening...Press enter to exit."")
                Console.ReadLine();
                //host.Exam();
                app.StopAsync();";

        public const string Directives = @"using CoreWCF;
                            using CoreWCF.Configuration;
                            using CoreWCF.Description;
                            using CoreWCF.Security;
                            using Microsoft.AspNetCore.Builder;
                            using Microsoft.AspNetCore.Hosting;
                            using Microsoft.Extensions.DependencyInjection;";

        public const string Directives2 = @"using CoreWCF;
                            using Microsoft.AspNetCore.Builder;
                            using CoreWCF.Configuration;
                            using CoreWCF.Description;
                            using CoreWCF.Security;
                            using Microsoft.AspNetCore.Hosting;
                            using Microsoft.Extensions.DependencyInjection;";

        private readonly NullLogger<SourceCodeUpdater> _logger = NullLogger<SourceCodeUpdater>.Instance;

        [Theory]
        [InlineData("using System.ServiceModel.Security;", Directives)]
        [InlineData("using System.ServiceModel.Security;using CoreWCF;\r\nusing Microsoft.AspNetCore.Builder;", Directives2)]
        public void UpdateDirectivesTest(string replace, string expected)
        {
            var updater = new SourceCodeUpdater(CSharpSyntaxTree.ParseText(Input.Replace("using System.ServiceModel.Security;", replace)), Template, _logger);
            var result = updater.UpdateDirectives().ToFullString().Replace(" ", string.Empty);
            var outdated = "using System.ServiceModel;using System.ServiceModel.Security;";
            Assert.DoesNotContain(outdated, result);
            Assert.Contains(expected.Replace(" ", string.Empty), result);
        }

        [Fact]
        public void ConfigureServiceHostTest()
        {
            var root = CSharpSyntaxTree.ParseText(Input);
            var updater = new SourceCodeUpdater(root, Template, _logger);
            var result = updater.AddTemplateCode(root.GetRoot()).ToFullString().Replace(" ", string.Empty);
            string config = @"serviceBuilder.ConfigureServiceHostBase<SampleService>(host =>
                { 
                    host.AddDefaultEndpoints(); 
                });".Replace(" ", string.Empty);
            Assert.Contains(config, result);
        }

        [Theory]
        [InlineData(Input, Added)]
        [InlineData(InputWithUsing, Added)]
        public void UpdateOpenCloseTest(string input, string added)
        {
            var root1 = CSharpSyntaxTree.ParseText(input);
            var root2 = CSharpSyntaxTree.ParseText(input.Replace("host.Close();", string.Empty));
            var result1 = new SourceCodeUpdater(root1, Template, _logger).AddTemplateCode(root1.GetRoot()).ToFullString().Replace(" ", string.Empty);
            var result2 = new SourceCodeUpdater(root2, Template, _logger).AddTemplateCode(root2.GetRoot()).ToFullString().Replace(" ", string.Empty);
            Assert.Contains(added.Replace(" ", string.Empty), result1);
            Assert.Contains(added.Replace(" ", string.Empty), result2);
        }

        [Fact]
        public void RelpaceNamesTest()
        {
            var updater = new SourceCodeUpdater(CSharpSyntaxTree.ParseText(Input), Template, _logger);
            var result = updater.UpdateDirectives().ToFullString().Replace(" ", string.Empty);
            Assert.Contains("host", result);
            Assert.Contains("SampleService", result);
            Assert.DoesNotContain("varName", result);
            Assert.DoesNotContain("ServiceType", result);
        }

        [Theory]
        [InlineData(Input)]
        [InlineData(InputWithUsing)]
        public void RemoveCodeTest(string input)
        {
            var end = @"app.StopAsync();
                        }".Replace(" ", string.Empty);
            var root = CSharpSyntaxTree.ParseText(input);
            var updater = new SourceCodeUpdater(root, Template, _logger);
            var result = updater.RemoveOldCode(updater.AddTemplateCode(root.GetRoot())).ToFullString().Replace(" ", string.Empty);
            Assert.Contains(end, result);
            Assert.DoesNotContain("int UA_placeHolder;", result);
        }

        [Fact]
        public void IsApplicableTest()
        {
            string notApplicableInput = @"using Serilog;
                using System;
                using System.IO;
                namespace SampleServer
                {
                    class Program
                    {
                        static void Main()
                        {
                            Console.Writeline(""Service Listening...Press enter to exit."")
                            Console.ReadLine();
                        }
                    }
                }";
            Assert.True(WCFUpdateChecker.IsSourceCodeApplicable(CSharpSyntaxTree.ParseText(Input)));
            Assert.False(WCFUpdateChecker.IsSourceCodeApplicable(CSharpSyntaxTree.ParseText(notApplicableInput)));
        }
    }
}
