// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public abstract class UpgradeCommand
    {
        /// <summary>
        /// A command that can be executed.
        /// </summary>
        public abstract Task<bool> ExecuteAsync(IUpgradeContext context, CancellationToken token);

        /// <summary>
        /// Gets the text displayed to the user from the REPL (e.g. Set Backup Path).
        /// </summary>
        public abstract string CommandText { get; }

        public bool IsEnabled { get; init; } = true;

        public static UpgradeCommand Create(string text, bool isEnabled = true)
            => Create(text, (_, __) => Task.FromResult(true), isEnabled);

        public static UpgradeCommand Create(string text, Action execute, bool isEnabled = true)
            => new DelegateUpgradeCommand(text, (_, __) =>
            {
                execute();
                return Task.FromResult(true);
            })
            { IsEnabled = isEnabled };

        public static UpgradeCommand Create(string text, Func<IUpgradeContext, CancellationToken, Task<bool>> execute, bool isEnabled = true)
            => new DelegateUpgradeCommand(text, execute) { IsEnabled = isEnabled };

        public static IEnumerable<UpgradeCommand<T>> CreateFromEnum<T>()
            where T : struct, Enum
            => Enum.GetValues<T>()
                .Select(t => new EnumUpgradeCommand<T>
                {
                    Value = t,
                });

        private class DelegateUpgradeCommand : UpgradeCommand
        {
            private readonly Func<IUpgradeContext, CancellationToken, Task<bool>> _execute;

            public DelegateUpgradeCommand(string text, Func<IUpgradeContext, CancellationToken, Task<bool>> execute)
            {
                _execute = execute;
                CommandText = text;
            }

            public override string CommandText { get; }

            public override Task<bool> ExecuteAsync(IUpgradeContext context, CancellationToken token)
                => _execute(context, token);
        }

        private class EnumUpgradeCommand<TEnum> : UpgradeCommand<TEnum>
            where TEnum : Enum
        {
            public override string CommandText => Value.ToString();

            public override Task<bool> ExecuteAsync(IUpgradeContext context, CancellationToken token)
                => Task.FromResult(true);
        }
    }
}
