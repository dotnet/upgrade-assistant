using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AspNetMigrator
{
    public abstract class MigrationStep
    {
        protected ILogger Logger { get; }

        protected MigrateOptions Options { get; }

        protected bool Initialized => Status != MigrationStepStatus.Unknown;

        public MigrationStep(MigrateOptions options, ILogger logger)
        {
            if (options is null || !options.IsValid())
            {
                throw new ArgumentException("Invalid migration options");
            }

            Options = options;
            Logger = logger;
            Status = MigrationStepStatus.Unknown;
            StatusDetails = string.Empty;
            Risk = BuildBreakRisk.Unknown;
            Commands = new List<MigrationCommand>();
        }

        public virtual string Title { get; protected set; } = string.Empty;

        public virtual string Description { get; protected set; } = string.Empty;

        public virtual MigrationStep? ParentStep { get; protected set; }

        public virtual List<MigrationCommand> Commands { get; set; }

        public virtual IEnumerable<MigrationStep> SubSteps { get; protected set; } = Enumerable.Empty<MigrationStep>();

        public MigrationStepStatus Status { get; private set; }

        public BuildBreakRisk Risk { get; set; }

        public bool IsComplete => Status switch
        {
            MigrationStepStatus.Complete => true,
            MigrationStepStatus.Skipped => true,
            _ => false,
        };

        public string StatusDetails { get; private set; }

        /// <summary>
        /// Implementers should use this method to initialize Status and any other state needed.
        /// </summary>
        protected abstract Task<MigrationStepInitializeResult> InitializeImplAsync(IMigrationContext context, CancellationToken token);

        /// <summary>
        /// Implementers should use this method to apply the migration step and return updated status and status details.
        /// </summary>
        protected abstract Task<MigrationStepApplyResult> ApplyImplAsync(IMigrationContext context, CancellationToken token);

        /// <summary>
        /// Initialize the migration step, including checking whether it is already complete and setting up necessary internal state.
        /// </summary>
        public virtual async Task InitializeAsync(IMigrationContext context, CancellationToken token)
        {
            try
            {
                (Status, StatusDetails, Risk) = await InitializeImplAsync(context, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception e)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                (Status, StatusDetails) = (MigrationStepStatus.Failed, "Unexpected error initializing step.");
                Logger.LogError(e, "Unexpected error initializing step");
            }
        }

        /// <summary>
        /// Apply migration and update Status as necessary.
        /// </summary>
        /// <returns>True if the migration step was successfully applied or false if migration failed.</returns>
        public virtual async Task<bool> ApplyAsync(IMigrationContext context, CancellationToken token)
        {
            if (!Initialized)
            {
                throw new InvalidOperationException("Migration steps must be initialized before they are applied");
            }

            if (Status == MigrationStepStatus.Complete)
            {
                return true;
            }

            Logger.LogInformation("Applying migration step {StepTitle}", Title);

            try
            {
                (Status, StatusDetails) = await ApplyImplAsync(context, token).ConfigureAwait(false);

                if (Status == MigrationStepStatus.Complete)
                {
                    Logger.LogInformation("Migration step {StepTitle} applied successfully", Title);
                    return true;
                }
                else
                {
                    Logger.LogWarning("Migration step {StepTitle} failed: {Status}: {StatusDetail}", Title, Status, StatusDetails);
                    return false;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception e)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                (Status, StatusDetails) = (MigrationStepStatus.Failed, "Unexpected error applying step.");
                Logger.LogError(e, "Unexpected error applying step");
                return false;
            }
        }

        /// <summary>
        /// Skips a migration step.
        /// </summary>
        /// <returns>True if the step was successfully skipped, false otherwise.</returns>
        public virtual Task<bool> SkipAsync(CancellationToken token)
        {
            // This method doesn't really need to be async or return a bool, as written now.
            // I've implemented it this way for now, though, to match ApplyAsync in case inheritors
            // want to change the behavior.
            Logger.LogInformation("Skipping migration step {StepTitle}", Title);
            (Status, StatusDetails) = (MigrationStepStatus.Skipped, "Step skipped");
            Logger.LogInformation("Migration step {StepTitle} skipped", Title);
            return Task.FromResult(true);
        }
    }
}
