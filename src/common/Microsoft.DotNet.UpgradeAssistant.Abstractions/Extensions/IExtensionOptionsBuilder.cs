// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    public interface IExtensionOptionsBuilder<TOption>
        where TOption : class, new()
    {
        /// <summary>
        /// Maps file paths given in <typeparamref name="TOption"/> to options of <typeparamref name="TTo"/>.
        ///
        /// Can be accessed via the following patterns:
        /// - <see cref="IOptions{OptionCollection{TTo}}"/>
        /// - <see cref="IOptions{OptionCollection{FileOption{TTo}}}"/>.
        /// </summary>
        /// <typeparam name="TTo">Type mapped file should be deserialized to.</typeparam>
        /// <param name="factory">Method to retrieve file paths.</param>
        void MapFiles<TTo>(Func<TOption, IEnumerable<string>> factory);

        /// <summary>
        /// Maps file paths given in <typeparamref name="TOption"/> to options of <typeparamref name="TTo"/>.
        ///
        /// Can be accessed via the following patterns:
        /// - <see cref="IOptions{OptionCollection{TTo}}"/>
        /// - <see cref="IOptions{OptionCollection{FileOption{TTo}}}"/>.
        /// </summary>
        /// <typeparam name="TTo">Type mapped file should be deserialized to.</typeparam>
        /// <param name="factory">Method to retrieve file path.</param>
        void MapFiles<TTo>(Func<TOption, string?> factory);
    }
}
