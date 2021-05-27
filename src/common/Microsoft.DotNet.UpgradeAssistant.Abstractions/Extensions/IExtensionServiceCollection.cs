// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    public interface IExtensionServiceCollection
    {
        /// <summary>
        /// Gets the configuration of the local extension.
        /// </summary>
        IConfiguration Configuration { get; }

        /// <summary>
        /// Gets the service collection.
        /// </summary>
        IServiceCollection Services { get; }

        /// <summary>
        /// Gets the file provider for the root of the extension.
        /// </summary>
        IFileProvider Files { get; }

        /// <summary>
        /// Add options that are supplied within an extension manifest.
        ///
        /// These options can be accessed via the following patterns:
        /// - <see cref="IOptions{TOption}"/>
        /// - <see cref="IOptions{OptionCollection{TOption}}"/>
        /// - <see cref="IOptions{OptionCollection{FileOption{TOption}}}"/>.
        /// </summary>
        /// <typeparam name="TOption">Option to bind configuration to.</typeparam>
        /// <param name="sectionName">Name in manifest to bind.</param>
        /// <returns>A builder to configure the options.</returns>
        IExtensionOptionsBuilder<TOption> AddExtensionOption<TOption>(string sectionName)
            where TOption : class, new();
    }
}
