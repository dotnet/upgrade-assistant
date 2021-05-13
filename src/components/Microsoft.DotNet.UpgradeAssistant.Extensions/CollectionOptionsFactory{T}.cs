// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    internal class CollectionOptionsFactory<T> : OptionsFactory<ICollection<T>>
    {
        public CollectionOptionsFactory(
            IEnumerable<IConfigureOptions<ICollection<T>>> setups,
            IEnumerable<IPostConfigureOptions<ICollection<T>>> postConfigures,
            IEnumerable<IValidateOptions<ICollection<T>>> validations)
            : base(setups, postConfigures, validations)
        {
        }

        protected override ICollection<T> CreateInstance(string name)
            => new List<T>();
    }
}
