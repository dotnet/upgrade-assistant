using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using AspNetMigrator.Reporting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AspNetMigrator.ConsoleApp
{
    [SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "No sync context in console apps")]
    public class ConsoleAnalyze : IHostedService
    {
        private readonly ILogger<ConsoleAnalyze> _logger;
        private readonly IMigrationContextFactory _factory;
        private readonly IReportGenerator _reportGenerator;
        private readonly IHostApplicationLifetime _lifetime;

        public ConsoleAnalyze(
            ILogger<ConsoleAnalyze> logger,
            IMigrationContextFactory factory,
            IReportGenerator reportGenerator,
            IHostApplicationLifetime lifetime)
        {
            _logger = logger;
            _factory = factory;
            _reportGenerator = reportGenerator;
            _lifetime = lifetime;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Starting report");

                var context = await _factory.CreateContext(cancellationToken);
                var visitor = new ConsolePageVisitor();

                await foreach (var page in _reportGenerator.Generate(context, cancellationToken))
                {
                    visitor.Visit(page);
                }
            }
            catch (MigrationException e)
            {
                _logger.LogError("Unexpected error: {Message}", e.Message);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                _lifetime.StopApplication();
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;

        private class ConsolePageVisitor : PageVisitor
        {
            public override void Visit(Table table)
            {
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
