// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using AutoMapper;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    internal class AggregateExtensionConfigureOptions<TOption> : IConfigureOptions<TOption>, IConfigureOptions<OptionCollection<TOption>>, IConfigureOptions<OptionCollection<FileOption<TOption>>>
        where TOption : class, new()
    {
        private readonly string _sectionName;
        private readonly IEnumerable<IExtension> _extensions;
        private readonly Lazy<Mapper> _mapper;

        public AggregateExtensionConfigureOptions(string sectionName, IEnumerable<IExtension> extensions)
        {
            _sectionName = sectionName;
            _extensions = extensions;
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

        public void Configure(OptionCollection<TOption> options)
             => Configure(options, AddFiles);

        public void Configure(OptionCollection<FileOption<TOption>> options)
            => Configure(options, static (e, o) => new FileOption<TOption>
            {
                Files = e.Files,
                Value = AddFiles(e, o)
            });

        private void Configure<T>(OptionCollection<T> options, Func<IExtension, TOption, T> factory)
        {
            foreach (var extension in _extensions)
            {
                var newOptions = extension.GetOptions<TOption>(_sectionName);

                if (newOptions is not null)
                {
                    options.Value.Add(factory(extension, newOptions));
                }
            }
        }

        private static TOption AddFiles(IExtension extension, TOption options)
        {
            if (options is IFileOption fileOption)
            {
                fileOption.Files = extension.Files;
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
