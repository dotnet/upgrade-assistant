// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public abstract class UpgradeStep
    {
        private bool Initialized => Status != UpgradeStepStatus.Unknown;

        protected ILogger Logger { get; }

        protected string? ProjectInitializedAgainst { get; set; }

        protected UpgradeStep(ILogger logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            ProjectInitializedAgainst = null;

            Status = UpgradeStepStatus.Unknown;
            StatusDetails = string.Empty;
            Risk = BuildBreakRisk.Unknown;
        }

        /// <summary>
        /// Gets a string that uniquely identifies this upgrade step.
        /// </summary>
        public virtual string Id => GetType().FullName!;

        /// <summary>
        /// Gets a user-friendly display name for the upgrade step.
        /// </summary>
        public abstract string Title { get; }

        /// <summary>
        /// Gets a user-friendly description of what the upgrade step does.
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// Gets or sets the upgrade step (if any) that this upgrade step is a sub-step of.
        /// </summary>
        public virtual UpgradeStep? ParentStep { get; protected set; }

        /// <summary>
        /// Gets or sets the upgrade steps that are sub-steps of this upgrade step. SubSteps are executed as part of executing the parent step.
        /// </summary>
        public virtual IEnumerable<UpgradeStep> SubSteps { get; protected set; } = Enumerable.Empty<UpgradeStep>();

        /// <summary>
        /// Gets the IDs of upgrade steps that must run before this upgrade step executes. 'DependsOn' steps are not children or parents (in that the steps don't contain one another). Instead, these are separate upgrade steps that must execute before this upgrade step can execute.
        /// </summary>
        /// <remarks>
        /// Dependencies are of type string so that upgrade steps can specify dependencies which may or may not be in used without needing to have a reference to those steps' assemblies.
        /// </remarks>
        public virtual IEnumerable<string> DependsOn => Enumerable.Empty<string>();

        /// <summary>
        /// Gets the IDs of upgrade steps that must not run before this upgrade step executes. 'DependencyOf' steps are not children or parents (in that the steps don't contain one another). Instead, these are separate upgrade steps that must not execute before this upgrade step can execute.
        /// </summary>
        /// <remarks>
        /// Dependencies are of type string so that upgrade steps can specify dependencies which may or may not be in used without needing to have a reference to those steps' assemblies.
        /// </remarks>
        public virtual IEnumerable<string> DependencyOf => Enumerable.Empty<string>();

        /// <summary>
        /// Gets the upgrade step's execution status.
        /// </summary>
        public UpgradeStepStatus Status { get; private set; }

        /// <summary>
        /// Gets the risk that executing the upgrade step will introduce a build break.
        /// </summary>
        public BuildBreakRisk Risk { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the upgrade is done (has completed successfully, failed, or was skipped).
        /// </summary>
        public bool IsDone => Status switch
        {
            UpgradeStepStatus.Complete => true,
            UpgradeStepStatus.Skipped => true,
            _ => false,
        };

        /// <summary>
        /// Gets a detailed status message suitable for displaying to the user explaining the upgrade step's current state.
        /// </summary>
        public string StatusDetails { get; private set; }

        /// <summary>
        /// Implementers should use this method to indicate whether the upgrade step applies to a given upgrade context.
        /// Note that applicability is not about whether the step is complete or not (InitializeImplAsync should check that),
        /// rather it is about whether it would ever make sense to run the migration step on the given context or not.
        /// For example, a upgrade step that acts at the project level would be inapplicable when a solution is selected
        /// rather than a project.
        /// </summary>
        /// <param name="context">The upgrade context to evaluate.</param>
        /// <returns>True if the upgrade step makes sense to evaluate and display for the given context, false otherwise.</returns>
        /// <param name="token">A cancellation token.</param>
        protected abstract Task<bool> IsApplicableImplAsync(IUpgradeContext context, CancellationToken token);

        /// <summary>
        /// Implementers should use this method to initialize Status and any other state needed. This method will be called when determining whether
        /// this step should be the next to execute. Returning Complete means that the step does not need to make any changes and can be skipped whereas
        /// returning incomplete means that there are changes the step needs to apply.
        /// </summary>
        protected abstract Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token);

        /// <summary>
        /// Implementers should use this method to apply the upgrade step and return updated status and status details.
        /// </summary>
        protected abstract Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token);

        /// <summary>
        /// Initialize the upgrade step, including checking whether it is already complete and setting up necessary internal state.
        /// </summary>
        public virtual async Task InitializeAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            try
            {
                ProjectInitializedAgainst = context.CurrentProject?.FileInfo?.FullName;
                (Status, StatusDetails, Risk) = await InitializeImplAsync(context, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (UpgradeException)
            {
                throw;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception e)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                (Status, StatusDetails) = (UpgradeStepStatus.Failed, "Unexpected error initializing step.");
                Logger.LogError(e, "Unexpected error initializing step");
                context.Telemetry?.TrackException(e);
            }
        }

        /// <summary>
        /// Apply upgrade and update Status as necessary.
        /// </summary>
        /// <returns>True if the upgrade step was successfully applied or false if upgrade failed.</returns>
        public virtual async Task<bool> ApplyAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!Initialized)
            {
                throw new InvalidOperationException("Upgrade steps must be initialized before they are applied");
            }

            if (Status == UpgradeStepStatus.Complete)
            {
                return true;
            }

            Logger.LogInformation("Applying upgrade step {StepTitle}", Title);

            try
            {
                (Status, StatusDetails) = await ApplyImplAsync(context, token).ConfigureAwait(false);

                if (Status == UpgradeStepStatus.Complete)
                {
                    Logger.LogInformation("Upgrade step {StepTitle} applied successfully", Title);
                    return true;
                }
                else
                {
                    Logger.LogWarning("Upgrade step {StepTitle} failed: {Status}: {StatusDetail}", Title, Status, StatusDetails);
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
                (Status, StatusDetails) = (UpgradeStepStatus.Failed, $"Unexpected error applying upgrade step '{Title}'");
                Logger.LogError(e, "Unexpected error applying upgrade step {StepTitle}", Title);
                context.AddResultForStep(this, context.CurrentProject?.GetFile()?.FilePath ?? string.Empty, Status, StatusDetails, details: e.ToString(), outputLevel: OutputLevel.Error);
                context.Telemetry?.TrackException(e);
                return false;
            }
        }

        /// <summary>
        /// Checks whether the upgrade step applies to the current upgrade context and, if it does,
        /// checks whether the any existing upgrade step status is still valid or whether the context
        /// has changed sufficiently that the upgrade step status should be reset.
        /// </summary>
        /// <remarks>This method is called every time the upgrade manager gets the next step, so it's important that it run
        /// as quickly as possible. It doesn't need to determine if a step needs to run or not, rather just whether it *might*
        /// be interesting for a given project so that the tool knows whether to include it in the list of upgrade steps or not.</remarks>
        /// <param name="context">The upgrade context to evaluate.</param>
        /// <returns>True if the upgrade step makes sense to evaluate and display for the given context, false otherwise.</returns>
        /// <param name="token">A cancellation token.</param>
        public virtual async Task<bool> IsApplicableAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var ret = await IsApplicableImplAsync(context, token).ConfigureAwait(false);

            // If the upgrade step applies, but it was previously initialized against a sufficiently different context, reset
            if (ret && Initialized && ShouldReset(context))
            {
                Logger.LogDebug("Resetting upgrade step {UpgradeStep} because upgrade context has changed significantly since it was initialized", Id);
                Reset();
            }

            return ret;
        }

        /// <summary>
        /// Determines whether the upgrade context has changed sufficiently since the upgrade step was initialized
        /// to warrant resetting the upgrade step's status.
        /// </summary>
        /// <param name="context">The upgrade context to evaluate.</param>
        /// <returns>True if the upgrade step should reset its status, otherwise false.</returns>
        protected virtual bool ShouldReset(IUpgradeContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!Initialized)
            {
                return false;
            }

            var currentProject = context.CurrentProject?.FileInfo.FullName;

            if (ProjectInitializedAgainst is null)
            {
                return currentProject is not null;
            }

            return !ProjectInitializedAgainst.Equals(currentProject, StringComparison.Ordinal);
        }

        /// <summary>
        /// Resets upgrade step status as if the step had not yet been initialized or applied. Useful for re-running a upgrade step when upgrade context changes.
        /// Upgrade steps that have additional internal state should override this method to reset that state (effectively undoing changes made by InitializeImplAsync
        /// and ApplyImplAsync).
        /// </summary>
        public virtual UpgradeStepInitializeResult Reset()
        {
            Status = UpgradeStepStatus.Unknown;
            StatusDetails = string.Empty;
            Risk = BuildBreakRisk.Unknown;
            ProjectInitializedAgainst = null;

            foreach (var subStep in SubSteps)
            {
                subStep.Reset();
            }

            return new UpgradeStepInitializeResult(Status, StatusDetails, Risk);
        }

        /// <summary>
        /// Skips a upgrade step.
        /// </summary>
        /// <returns>True if the step was successfully skipped, false otherwise.</returns>
        public virtual Task<bool> SkipAsync(CancellationToken token)
        {
            // This method doesn't really need to be async or return a bool, as written now.
            // I've implemented it this way for now, though, to match ApplyAsync in case inheritors
            // want to change the behavior.
            Logger.LogInformation("Skipping upgrade step {StepTitle}", Title);
            (Status, StatusDetails) = (UpgradeStepStatus.Skipped, "Step skipped");
            Logger.LogInformation("Upgrade step {StepTitle} skipped", Title);
            return Task.FromResult(true);
        }
    }
}
