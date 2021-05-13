// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Templates
{
    /// <summary>
    /// Internal type that supplements an ItemSpec with replacements relevant to its template configuration,
    /// the extension an item comes from, and the relative path to the template file corresponding to the item.
    /// </summary>
    internal record RuntimeItemSpec : ItemSpec
    {
        private readonly IFileProvider _files;

        public RuntimeItemSpec(ItemSpec baseItem, IFileProvider files, Dictionary<string, string> replacements)
            : base(baseItem.Type, baseItem.Path, baseItem.Keywords.ToArray())
        {
            _files = files;
            Replacements = ImmutableDictionary.CreateRange(replacements);
        }

        /// <summary>
        /// Gets a dictionary mapping text in the template file
        /// with text that should replace it.
        /// </summary>
        public ImmutableDictionary<string, string>? Replacements { get; }

        public Stream? OpenRead()
        {
            if (_files is null)
            {
                return null;
            }

            return _files.GetFileInfo(Path).CreateReadStream();
        }
    }
}
