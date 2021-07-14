// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using AutoMapper;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    internal class AggregateExtensionConfigureOptions<TOption> : IConfigureOptions<TOption>, IConfigureOptions<ICollection<TOption>>, IConfigureOptions<ICollection<FileOption<TOption>>>
        where TOption : class, new()
    {
        private readonly string _sectionName;
        private readonly IEnumerable<ExtensionInstance> _extensions;
        private readonly Lazy<Mapper> _mapper;

        public AggregateExtensionConfigureOptions(string sectionName, IExtensionManager extensions)
        {
            _sectionName = sectionName;
            _extensions = extensions.Instances;
            _mapper = new Lazy<Mapper>(() => GetMapper());
        }

        public void Configure(TOption options)
        {
            foreach (var extension in _extensions)
            {
                var newOptions = extension.GetOptions<TOption>(_sectionName);

                if (newOptions is not null)
                {
                    _mapper.Value.Map(newOptions, options);
                }
            }
        }

        public void Configure(ICollection<TOption> options)
             => Configure(options, AddFiles);

        public void Configure(ICollection<FileOption<TOption>> options)
            => Configure(options, static (e, o) => new FileOption<TOption>
            {
                Files = e.FileProvider,
                Value = AddFiles(e, o)
            });

        private void Configure<T>(ICollection<T> options, Func<ExtensionInstance, TOption, T> factory)
        {
            foreach (var extension in _extensions)
            {
                var newOptions = extension.GetOptions<TOption>(_sectionName);

                if (newOptions is not null)
                {
                    options.Add(factory(extension, newOptions));
                }
            }
        }

        private static TOption AddFiles(ExtensionInstance extension, TOption options)
        {
            if (options is IFileOption fileOption)
            {
                fileOption.Files = extension.FileProvider;
            }

            return options;
        }

        private static Mapper GetMapper()
        {
            var config = new MapperConfiguration(config =>
             {
                 config.CreateMap<TOption, TOption>()
                     .ForAllMembers(options => options.Condition((src, dest, member) =>
                     {
                         // Don't overwrite older options with newer ones
                         // that are null of empty.
                         if (member is null)
                         {
                             return false;
                         }

                         if (member is Array a && a.Length == 0)
                         {
                             return false;
                         }

                         if (member is string s && s.Length == 0)
                         {
                             return false;
                         }

                         return true;
                     }));
             });

            return new Mapper(config);
        }
    }
}
