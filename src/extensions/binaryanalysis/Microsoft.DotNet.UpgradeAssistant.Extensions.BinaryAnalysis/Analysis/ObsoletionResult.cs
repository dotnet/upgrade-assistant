// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.Analysis
{
    public record ObsoletionResult
    {
        public ObsoletionResult(string message, Uri url)
        {
            ArgumentNullException.ThrowIfNull(message);
            ArgumentNullException.ThrowIfNull(url);

            Message = message;
            Url = url;
        }

        public string Message { get; }

        public Uri Url { get; }
    }
}
