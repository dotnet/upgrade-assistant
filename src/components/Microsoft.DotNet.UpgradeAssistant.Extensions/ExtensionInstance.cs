﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    [DebuggerDisplay("{Name}, {Location}")]
    public sealed class ExtensionInstance : IDisposable
    {
        private const string ExtensionServiceProvidersSectionName = "ExtensionServiceProviders";
        public const string ManifestFileName = "ExtensionManifest.json";

        private const string ExtensionNamePropertyName = "ExtensionName";
        private const string DefaultExtensionName = "Unknown";

        private readonly Lazy<AssemblyLoadContext>? _alc;

        public ExtensionInstance(IFileProvider fileProvider, string location)
        {
            FileProvider = fileProvider;
            Location = location;
            Configuration = CreateConfiguration(fileProvider);
            Name = GetName(Configuration, location);

            var serviceProviders = GetOptions<string[]>(ExtensionServiceProvidersSectionName);

            if (serviceProviders is not null)
            {
                _alc = new Lazy<AssemblyLoadContext>(() => new ExtensionAssemblyLoadContext(this, serviceProviders));
            }
        }

        public string Name { get; }

        public void AddServices(IServiceCollection services)
        {
            if (_alc is null)
            {
                return;
            }

            var serviceProviders = _alc.Value.Assemblies
                .SelectMany(assembly => assembly.GetTypes()
                .Where(t => t.IsPublic && !t.IsAbstract && typeof(IExtensionServiceProvider).IsAssignableFrom(t))
                .Select(t => Activator.CreateInstance(t))
                .Cast<IExtensionServiceProvider>());

            foreach (var sp in serviceProviders)
            {
                sp.AddServices(new ExtensionServiceCollection(services, this));
            }
        }

        public string Location { get; }

        public IFileProvider FileProvider { get; }

        public IConfiguration Configuration { get; }

        public T? GetOptions<T>(string sectionName) => Configuration.GetSection(sectionName).Get<T>();

        public void Dispose()
        {
            if (FileProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        private static string GetName(IConfiguration configuration, string location)
        {
            if (configuration[ExtensionNamePropertyName] is string name)
            {
                return name;
            }

            if (Path.GetFileNameWithoutExtension(location) is string locationName)
            {
                return locationName;
            }

            return DefaultExtensionName;
        }

        private static IConfiguration CreateConfiguration(IFileProvider fileProvider)
        {
            try
            {
                return new ConfigurationBuilder()
                    .AddJsonFile(fileProvider, ManifestFileName, optional: false, reloadOnChange: false)
                    .Build();
            }
            catch (FileNotFoundException e)
            {
                throw new UpgradeException("Could not find manifest file", e);
            }
        }
    }
}
