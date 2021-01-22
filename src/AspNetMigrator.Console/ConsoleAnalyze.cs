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
            MigrateOptions options,
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
                throw new NotImplementedException();
            }

            public override void Visit(Divider divider)
            {
                throw new NotImplementedException();
            }

            public override void Visit(Text text)
            {
                Console.WriteLine(text.Content);
            }
        }
    }
}
