using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AspNetMigrator.Engine
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
            Logger = logger ?? new NullLogger();
            Status = MigrationStepStatus.Unknown;
        }

        public virtual string Title { get; protected set; }

        public virtual string Description { get; protected set; }

        public virtual MigrationStep ParentStep { get; protected set; }

        public virtual IEnumerable<MigrationStep> SubSteps { get; protected set; } = Enumerable.Empty<MigrationStep>();

        public MigrationStepStatus Status { get; private set; }

        public string StatusDetails { get; private set; }

        /// <summary>
        /// Implementers should use this method to initialize Status and any other state needed.
        /// </summary>
        protected abstract Task<(MigrationStepStatus Status, string StatusDetails)> InitializeImplAsync();

        /// <summary>
        /// Implementers should use this method to apply the migration step and return updated status and status details.
        /// </summary>
        protected abstract Task<(MigrationStepStatus Status, string StatusDetails)> ApplyImplAsync();

        /// <summary>
        /// Initialize the migration step, including checking whether it is already complete and setting up necessary internal state.
        /// </summary>
        public async Task InitializeAsync()
        {
            (Status, StatusDetails) = await InitializeImplAsync();
        }

        /// <summary>
        /// Apply migration and update Status as necessary.
        /// </summary>
        /// <returns>True if the migration step was successfully applied or false if migration failed.</returns>
        public async Task<bool> ApplyAsync()
        {
            if (!Initialized)
            {
                throw new InvalidOperationException("Migration steps must be initialized before they are applied");
            }

            if (Status == MigrationStepStatus.Complete)
            {
                return true;
            }

            (Status, StatusDetails) = await ApplyImplAsync();

            return Status == MigrationStepStatus.Complete;
        }
    }
}
