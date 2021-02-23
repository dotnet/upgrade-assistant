// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Reporting;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Cli
{
    public class ConsoleAnalyze : IAppCommand
    {
        private readonly ILogger<ConsoleAnalyze> _logger;
        private readonly IUpgradeContextFactory _factory;
        private readonly IReportGenerator _reportGenerator;

        public ConsoleAnalyze(
            ILogger<ConsoleAnalyze> logger,
            IUpgradeContextFactory factory,
            IReportGenerator reportGenerator)
        {
            _logger = logger;
            _factory = factory;
            _reportGenerator = reportGenerator;
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting report");

            var context = await _factory.CreateContext(cancellationToken);
            var visitor = new ConsolePageVisitor();

            await foreach (var page in _reportGenerator.Generate(context, cancellationToken))
            {
                visitor.Visit(page);
            }
        }

        private class ConsolePageVisitor : PageVisitor
        {
            public override void Visit(Table table)
            {
                var divider = new string('-', Console.WindowWidth / 2);

                Console.WriteLine(divider);
                Console.WriteLine(string.Join("\t", table.Headers));
                Console.WriteLine(divider);
                Console.WriteLine();
                foreach (var row in table.Rows)
                {
                    Console.WriteLine(string.Join("\t", row.Data));
                }
            }

            public override void Visit(Divider divider)
            {
                Console.WriteLine();
            }

            public override void Visit(Text text)
            {
                Console.WriteLine(text.Content);
            }

            public override void Visit(Page page)
            {
                Console.WriteLine(new string('-', Console.WindowWidth));
                Console.WriteLine($"Project: {page.Title}");
                Console.WriteLine(new string('-', Console.WindowWidth));
                Console.WriteLine();
                base.Visit(page);
            }

            public override void Visit(Section section)
            {
                var divider = new string('-', Console.WindowWidth / 2);

                Console.WriteLine(divider);
                Console.WriteLine($"Section: {section.Header}");
                Console.WriteLine(divider);

                foreach (var child in section.Content)
                {
                    Visit(child);
                }
            }
        }
    }
}
