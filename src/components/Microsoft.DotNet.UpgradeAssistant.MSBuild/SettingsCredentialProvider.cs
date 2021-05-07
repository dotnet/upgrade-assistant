// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Adapted from https://github.com/NuGet/NuGet.Client/blob/17c4f841ff61d27fe6b57cf45ceef16037062635/src/NuGet.Clients/NuGet.CommandLine/SettingsCredentialProvider.cs
using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Credentials;

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild
{
    public class SettingsCredentialProvider : ICredentialProvider
    {
        private readonly IPackageSourceProvider _packageSourceProvider;
        private readonly ILogger _logger;

        public string Id { get; }

        public SettingsCredentialProvider(IPackageSourceProvider packageSourceProvider, ILogger logger)
        {
            _packageSourceProvider = packageSourceProvider ?? throw new ArgumentNullException(nameof(packageSourceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Id = $"{typeof(SettingsCredentialProvider).Name}_{Guid.NewGuid()}";
        }

        public Task<CredentialResponse> GetAsync(
            Uri uri,
            IWebProxy proxy,
            CredentialRequestType type,
            string message,
            bool isRetry,
            bool nonInteractive,
            CancellationToken cancellationToken)
        {
            if (uri is null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            cancellationToken.ThrowIfCancellationRequested();

            var cred = GetCredentials(uri, type, isRetry);

            var response = cred != null
                ? new CredentialResponse(cred)
                : new CredentialResponse(CredentialStatus.ProviderNotApplicable);

            return Task.FromResult(response);
        }

        private ICredentials? GetCredentials(Uri uri, CredentialRequestType credentialType, bool retrying)
        {
            // If we are retrying, the stored credentials must be invalid.
            if (!retrying && (credentialType != CredentialRequestType.Proxy) && TryGetCredentials(uri, out var credentials, out var username))
            {
                _logger.LogMinimal(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        "Using credentials from config. UserName: {0}",
                        username));
                return credentials;
            }

            return null;
        }

        private bool TryGetCredentials(Uri uri, out ICredentials? configurationCredentials, out string? username)
        {
            var source = _packageSourceProvider.LoadPackageSources().FirstOrDefault(p =>
            {
                return p.Credentials != null
                    && p.Credentials.IsValid()
                    && Uri.TryCreate(p.Source, UriKind.Absolute, out var sourceUri)
                    && UriEquals(sourceUri, uri);
            });

            if (source == null)
            {
                // The source is not in the config file
                configurationCredentials = null;
                username = null;
                return false;
            }

            configurationCredentials = source.Credentials.ToICredentials();
            username = source.Credentials.Username;
            return true;
        }

        private static bool UriEquals(Uri uri1, Uri uri2)
        {
            uri1 = CreateODataAgnosticUri(uri1.OriginalString.TrimEnd('/'));
            uri2 = CreateODataAgnosticUri(uri2.OriginalString.TrimEnd('/'));

            return Uri.Compare(uri1, uri2, UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.SafeUnescaped, StringComparison.OrdinalIgnoreCase) == 0;
        }

        private static Uri CreateODataAgnosticUri(string uri)
        {
            if (uri.EndsWith("$metadata", StringComparison.OrdinalIgnoreCase))
            {
                uri = uri[0..^9].TrimEnd('/');
            }

            return new Uri(uri);
        }
    }
}
