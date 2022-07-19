// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.UpgradeAssistant.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant.Cli
{
    public class UpgradeAssistantCommandOptions : BaseUpgradeAssistantOptions
    {
        private bool _servicesConfigured;

        public FileInfo Project { get; set; } = null!;

        // Name must be Extension and not plural as the name of the argument that it binds to is `--extension`
        public IReadOnlyCollection<string> Extension { get; set; } = Array.Empty<string>();

        // Name must be EntryPoint and not plural as the name of the argument that it binds to is `--entry-point`
        public IReadOnlyCollection<string> EntryPoint { get; set; } = Array.Empty<string>();

        public IReadOnlyCollection<string> Option { get; set; } = Array.Empty<string>();

        public bool IgnoreUnsupportedFeatures { get; set; }

        public IEnumerable<AdditionalOption> AdditionalOptions => Option.ParseOptions();

        public DirectoryInfo? VSPath { get; set; }

        public DirectoryInfo? MSBuildPath { get; set; }

        public void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (!_servicesConfigured)
            {
                services.AddSingleton(this);

                var extensionOptionsBuilder = new OptionsBuilder<ExtensionOptions>(services, null)
                    .Configure(options =>
                        {
                            options.AdditionalOptions = this.AdditionalOptions;
                            options.CheckMinimumVersion = !UpgradeVersion.Current.IsDevelopment;
                            options.CurrentVersion = UpgradeVersion.Current.Version;

                            foreach (var path in this.Extension)
                            {
                                options.ExtensionPaths.Add(path);
                            }
                        });

                services.AddSingleton<IUpgradeStateManager, FileUpgradeStateFactory>();
                services
                    .AddOptions<FileStateOptions>()
                    .Configure(options =>
                    {
                        if (this.Project?.DirectoryName is string directory)
                        {
                            options.Path = Path.Combine(directory, ".upgrade-assistant");
                        }
                    })
                    .ValidateDataAnnotations();

                services.AddMsBuild(msBuildOptions =>
                {
                    if (this.Project?.FullName is string fullname)
                    {
                        msBuildOptions.InputPath = fullname;
                    }

                    if (this.VSPath?.FullName is string vspath)
                    {
                        msBuildOptions.VisualStudioPath = vspath;
                    }

                    if (this.MSBuildPath?.FullName is string msbuildPath)
                    {
                        msBuildOptions.MSBuildPath = msbuildPath;
                    }
                });

                services.AddReadinessChecks(options =>
                {
                    options.IgnoreUnsupportedFeatures = this.IgnoreUnsupportedFeatures;
                });

                _servicesConfigured = true;
            }
        }
    }
}
