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
        private bool Initialized => Status != MigrationStepStatus.Unknown;

        protected ILogger Logger { get; }

        public MigrationStep(ILogger logger)
        {
            Logger = logger;
            Commands = new List<MigrationCommand>();

            Status = MigrationStepStatus.Unknown;
            StatusDetails = string.Empty;
            Risk = BuildBreakRisk.Unknown;
        }

        /// <summary>
        /// Gets a string that uniquely identifies this migration step.
        /// </summary>
        public abstract string Id { get; }

        /// <summary>
        /// Gets a user-friendly display name for the migration step.
        /// </summary>
        public abstract string Title { get; }

        /// <summary>
        /// Gets a user-friendly description of what the migration step does.
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// Gets or sets the migration step (if any) that this migration step is a sub-step of.
        /// </summary>
        public virtual MigrationStep? ParentStep { get; protected set; }

        /// <summary>
        /// Gets or sets migration step-specific commands that the user can choose from in addition to the migrator's default commands.
        /// </summary>
        public virtual List<MigrationCommand> Commands { get; set; }

        /// <summary>
        /// Gets or sets the migration steps that are sub-steps of this migration step. SubSteps are executed as part of executing the parent step.
        /// </summary>
        public virtual IEnumerable<MigrationStep> SubSteps { get; protected set; } = Enumerable.Empty<MigrationStep>();

        /// <summary>
        /// Gets the full type names of migration steps that must run before this migration step executes. 'ExecuteAfter' steps are not children or parents (in that the steps don't contain one another). Instead, these are separate migration steps that must execute before this migration step can execute.
        /// </summary>
        /// <remarks>Type names are stored as strings so that migration step assemblies don't need to depend on other steps if the only relationship between them is a pre-req. However, using a migration step without also using its ExecuteAfter steps will result in a runtime exception.</remarks>
        public virtual IEnumerable<string> ExecuteAfter => Enumerable.Empty<string>();

        /// <summary>
        /// Gets the full type names of migration steps that must not run before this migration step executes. 'ExecuteBefore' steps are not children or parents (in that the steps don't contain one another). Instead, these are separate migration steps that must not execute before this migration step can execute.
        /// </summary>
        /// <remarks>Type names are stored as strings so that migration step assemblies don't need to depend on other steps if the only relationship between them is a pre-req. Using a migration step without also using its ExecuteAfter steps is permissible.</remarks>
        public virtual IEnumerable<string> ExecuteBefore => Enumerable.Empty<string>();

        /// <summary>
        /// Gets the migration step's execution status.
        /// </summary>
        public MigrationStepStatus Status { get; private set; }

        /// <summary>
        /// Gets the risk that executing the migration step will introduce a build break.
        /// </summary>
        public BuildBreakRisk Risk { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the migration is done (has completed successfully or was skipped).
        /// </summary>
        public bool IsDone => Status switch
        {
            MigrationStepStatus.Complete => true,
            MigrationStepStatus.Skipped => true,
            _ => false,
        };

        /// <summary>
        /// Gets a detailed status message suitable for displaying to the user explaining the migration step's current state.
        /// </summary>
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
            catch (Exception e)
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
            catch (Exception e)
            {
                (Status, StatusDetails) = (MigrationStepStatus.Failed, "Unexpected error applying step.");
                Logger.LogError(e, "Unexpected error applying step");
                return false;
            }
        }

        /// <summary>
        /// Resets migration step status as if the step had not yet been initialized or applied. Useful for re-running a migration step when migration context changes.
        /// </summary>
        public virtual MigrationStepInitializeResult Reset()
        {
            Status = MigrationStepStatus.Unknown;
            StatusDetails = string.Empty;
            Risk = BuildBreakRisk.Unknown;

            return new MigrationStepInitializeResult(Status, StatusDetails, Risk);
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
