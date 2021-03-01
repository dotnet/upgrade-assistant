// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant;
using Microsoft.DotNet.UpgradeAssistant.Extensions;
using Microsoft.Extensions.Logging;

namespace UpgradeStepSample
{
    /// <summary>
    /// Upgrade steps all derived from the Microsoft.DotNet.UpgradeAssistant.UpgradeStep base class.
    /// </summary>
    public class AuthorsPropertyUpgradeStep : UpgradeStep
    {
        private const string AuthorsPropertyName = "Authors";
        private const string AuthorsPropertySectionName = "AuthorsProperty";

        private readonly AuthorsPropertyOptions? _options;
        private readonly ILogger<AuthorsPropertyUpgradeStep> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorsPropertyUpgradeStep"/> class.
        /// An upgrade step's constructor should have an ILogger parameter along with parameters for any other
        /// services that need resolved from the dependency injection container. In extensions, AggregateExtension
        /// is a useful service for providing access to extensions' configuration (as read from extension manifests).
        /// </summary>
        /// <param name="logger">Used for logging diagnostic messages. Must be passed to the upgrade step's base class ctor.</param>
        /// <param name="aggregateExtension">A useful service that provides access to extensions' configuration settings.</param>
        public AuthorsPropertyUpgradeStep(AggregateExtension aggregateExtension, ILogger<AuthorsPropertyUpgradeStep> logger)
            : base(logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            if (aggregateExtension is null)
            {
                throw new ArgumentNullException(nameof(aggregateExtension));
            }

            // The AggregateExtension type can be used to get files and configuration settings from extensions.
            _options = aggregateExtension.GetOptions<AuthorsPropertyOptions>(AuthorsPropertySectionName);
        }

        // ***** ***** *****
        // Required overrides
        // ***** ***** *****

        /// <summary>
        /// Gets a unique ID string used for referring to the upgrade step from other steps.
        /// </summary>
        public override string Id => typeof(AuthorsPropertyUpgradeStep).FullName!;

        /// <summary>
        /// Gets a title that's displayed to the user when upgrade steps are listed.
        /// </summary>
        public override string Title => "Add an <Authors> property to the project";

        /// <summary>
        /// Gets a string describing in more detail the purpose of the upgrade step.
        /// </summary>
        public override string Description => "Updates the project file to include an <Authors> property for listing author information for a NuGet package if the <Authors> property is missing.";

        /// <summary>
        /// Returns true if the upgrade step is applicable to the current upgrade context state, otherwise false.
        /// If this returns true, the step will be shown in the list of steps to be applied. If this returns false,
        /// the step will not be shown in the list. This method is called every time an upgrade step is applied,
        /// so the applicability of the step may change over the course of the upgrade process.
        /// </summary>
        /// <param name="context">The upgrade context to evaluate for applicability.</param>
        /// <returns>True if the upgrade step should be displayed to the user; false otherwise.</returns>
        protected override bool IsApplicableImpl(IUpgradeContext context)
        {
            // Because this upgrade step works at the project level, it is only applicable if there is a current project available.
            // If, for example, the user hasn't selected which project to upgrade yet, then this step will not apply.
            return context?.CurrentProject is not null;
        }

        /// <summary>
        /// This method runs when the Upgrade Assistant is considering this upgrade step to run next. The method needs to
        /// determine whether the step should be run (or if, for example, it has no work to do) and return an
        /// appropriate UpgradeStepInitializeResult. This method can also prepare stat that the upgrade step
        /// will need to execute.
        /// </summary>
        /// <param name="context">The upgrade context to evaluate.</param>
        /// <param name="token">A cancellation token.</param>
        /// <returns>An UpgradeStepInitializeResult representing the current state of the upgrade step.</returns>
        protected override Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // The IProjectFile abstraction exposes the project file.
            var projectFile = context.CurrentProject.Required().GetFile();

            if (_options?.Authors is null)
            {
                _logger.LogDebug("No authors setting found in extension configuration. Authors will not be added to the project.");

                // UpgradeInitializeResult includes a step status, string with details on the status, and a BuildBreakRisk property
                // indicating to the user whether applying this step is likely to introduce build breaks into their project.
                var result = new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, "No authors need added", BuildBreakRisk.None);
                return Task.FromResult(result);
            }

            var existingAuthorsProperty = projectFile.GetPropertyValue(AuthorsPropertyName);
            if (existingAuthorsProperty is null || !existingAuthorsProperty.Contains(_options.Authors, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Existing authors property ({Authors}) does not contain expected authors ({ExpectedAuthors})", existingAuthorsProperty, _options.Authors);

                // If a change is needed, InitializeImplAsync should return a result with UpgradeStepStatus.Incomplete, but not make the update
                // until the upgrade step's ApplyImplAsync method is called.
                var result = new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, $"Expected authors {_options.Authors} need added to the projects Authors property", BuildBreakRisk.None);
                return Task.FromResult(result);
            }
            else
            {
                _logger.LogDebug("An existing Authors property exists with expected Authors. Skipping AuthorsPropertyUpgradeStep");
                var result = new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, "Expected authors are already listed in the project file", BuildBreakRisk.None);
                return Task.FromResult(result);
            }
        }

        /// <summary>
        /// The ApplyImplAsync step is invoked when the user attempts to actually apply the upgrade step and should make
        /// necessary changes to the upgrade context.
        /// </summary>
        /// <param name="context">The upgrade context to update.</param>
        /// <param name="token">A cancellation token.</param>
        /// <returns>A result indicating whether the upgrade step applied successfully or not.</returns>
        protected override async Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (_options?.Authors is null)
            {
                throw new InvalidOperationException("AuthorsPropertyUpgradeStep should not be applied without a valid authors option set.");
            }

            // The IProjectFile abstraction exposes the project file.
            var projectFile = context.CurrentProject.Required().GetFile();
            var existingAuthorsProperty = projectFile.GetPropertyValue(AuthorsPropertyName);

            var updatedPropertyValue = existingAuthorsProperty is null
                ? _options.Authors
                : string.Join(';', _options.Authors, existingAuthorsProperty);

            // IProjectFile.SetPropertyValue will add or update a property
            projectFile.SetPropertyValue(AuthorsPropertyName, updatedPropertyValue);

            // Any changes directly to the project file require saving the file (which will automatically reload the project
            // in the upgrade context).
            await projectFile.SaveAsync(token).ConfigureAwait(false);

            // ApplyImplAsync should return an instance of UpgradeStepApplyResult indicating whether the update was successful
            // and details on what was changed.
            _logger.LogDebug("Successfully updated project file's authors property to {Authors}", updatedPropertyValue);
            return new UpgradeStepApplyResult(UpgradeStepStatus.Complete, $"Authors added to the project's Authors property (new property value: {updatedPropertyValue})");
        }

        // ***** ***** *****
        // Optional overrides
        // ***** ***** *****

        /// <summary>
        /// Gets a list of upgrade step IDs representing upgrade steps that this step must execute after.
        /// This sample step should execute after a project has been selected.
        /// </summary>
        public override IEnumerable<string> DependsOn { get; } = new[]
        {
            // The user must select a specific project before updating the authors property
            "Microsoft.DotNet.UpgradeAssistant.Steps.Solution.CurrentProjectSelectionStep",
        };

        /// <summary>
        /// Gets a list of upgrade step IDs representing upgrade steps that this step must execute before.
        /// This sample step should execute before completing project upgrade and moving to the next project.
        /// </summary>
        public override IEnumerable<string> DependencyOf { get; } = new[]
        {
            "Microsoft.DotNet.UpgradeAssistant.Steps.Solution.NextProjectStep",
        };

        /// <summary>
        /// By default, upgrade steps' status resets when a new project is selected (so the same
        /// step can be applied to multiple projects in a single Upgrade Assistant session). If
        /// that heuristic is not correct for determining when to reset a given step's status,
        /// it can be changed by overriding ShouldReset.
        /// </summary>
        /// <param name="context">The upgrade context to make a decision about resetting the upgrade step for.</param>
        /// <returns>True if the upgrade step status should reset; false otherwise.</returns>
        protected override bool ShouldReset(IUpgradeContext context) => base.ShouldReset(context);

        /// <summary>
        /// Gets or sets the sub-steps for upgrade steps with sub-steps (like the sourcde updater step). If an upgrade
        /// step has children steps, the step should create them explicitly when it is created instead of depending
        /// on the dependency injection system to instantiate them.
        /// </summary>
        public override IEnumerable<UpgradeStep> SubSteps { get => base.SubSteps; protected set => base.SubSteps = value; }

        /// <summary>
        /// Gets or sets the parent step for a child step in a sub-step scenario (for example, the code fixer steps in
        /// source update scenarios have the source updater step as their parent).
        /// </summary>
        public override UpgradeStep? ParentStep { get => base.ParentStep; protected set => base.ParentStep = value; }
    }
}
