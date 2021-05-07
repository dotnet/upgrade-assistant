// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Adapted from https://github.com/NuGet/NuGet.Client/blob/17c4f841ff61d27fe6b57cf45ceef16037062635/src/NuGet.Clients/NuGet.CommandLine/Common/ConsoleCredentialProvider.cs
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Configuration;
using NuGet.Credentials;

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild
{
    public class ConsoleCredentialProvider : ICredentialProvider
    {
        public ConsoleCredentialProvider()
        {
            Id = $"{typeof(ConsoleCredentialProvider).Name}_{Guid.NewGuid()}";
        }

        /// <summary>
        /// Gets unique identifier of this credential provider.
        /// </summary>
        public string Id { get; }

        public Task<CredentialResponse> GetAsync(
            Uri uri,
            IWebProxy proxy,
            CredentialRequestType type,
            string message,
            bool isRetry,
            bool nonInteractive,
            CancellationToken cancellationToken)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            if (nonInteractive)
            {
                return Task.FromResult(
                    new CredentialResponse(CredentialStatus.ProviderNotApplicable));
            }

            message = type switch
            {
                CredentialRequestType.Proxy => "Please provide proxy credentials:",
                CredentialRequestType.Forbidden => "The remote server indicated that the previous request was forbidden. Please provide credentials for: {0}",
                _ => "Please provide credentials for: {0}",
            };
            Console.WriteLine(message, uri.OriginalString);

            Console.Write("UserName: ");
            cancellationToken.ThrowIfCancellationRequested();
            var username = Console.ReadLine();
            Console.Write("Password: ");
            cancellationToken.ThrowIfCancellationRequested();
            var password = Console.ReadLine();

            return Task.FromResult(new CredentialResponse(new NetworkCredential(username, password)));
        }
    }
}
