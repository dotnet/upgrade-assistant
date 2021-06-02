// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FindReplaceStep
{
    /// <summary>
    /// Upgrade steps derive from the Microsoft.DotNet.UpgradeAssistant.UpgradeStep base class.
    /// </summary>
    public class FindReplaceUpgradeStep : UpgradeStep
    {
        // A mapping for file paths to the strings in that file to be replaced.
        private Dictionary<string, IEnumerable<string>>? _neededReplacements;

        /// <summary>
        /// Gets the dictionary storing the strings that this upgrade step will replace.
        /// </summary>
        public Dictionary<string, string> StringsToReplace { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FindReplaceUpgradeStep"/> class.
        /// An upgrade step's constructor should have an ILogger parameter along with parameters for any other
        /// services that need resolved from the dependency injection container. In extensions, AggregateExtension
        /// is a useful service for providing access to extensions' configuration (as read from extension manifests).
        /// </summary>
        /// <param name="logger">Used for logging diagnostic messages. Must be passed to the upgrade step's base class ctor.</param>
        /// <param name="findReplaceOptionsCollection">Find/replace options as read from extension manifests. Because an ICollection is requested,
        /// this property will include FindReplaceOptions from all extensions.</param>
        public FindReplaceUpgradeStep(IOptions<ICollection<FindReplaceOptions>> findReplaceOptionsCollection, ILogger<FindReplaceUpgradeStep> logger)
            : base(logger)
        {
            if (findReplaceOptionsCollection is null)
            {
                throw new ArgumentNullException(nameof(findReplaceOptionsCollection));
            }

            // Reading configuration from AggregateExtension allows us to read configuration both from this extension's
            // manifest as well as from other extensions' manifests which may wish to further refine the behavior of this one.
            StringsToReplace = findReplaceOptionsCollection.Value.SelectMany(f => f.Replacements).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        // ***** ***** *****
        // Required overrides
        // ***** ***** *****

        /// <summary>
        /// Gets a title that's displayed to the user when upgrade steps are listed.
        /// </summary>
        public override string Title => "Replace strings in source files";

        /// <summary>
        /// Gets a string describing in more detail the purpose of the upgrade step.
        /// </summary>
        public override string Description => "Updates project source by applying configured string replacements.";

        /// <summary>
        /// Returns true if the upgrade step is applicable to the current upgrade context state, otherwise false.
        /// If this returns true, the step will be shown in the list of steps to be applied. If this returns false,
        /// the step will not be shown in the list. This method is called every time an upgrade step is applied,
        /// so the applicability of the step may change over the course of the upgrade process.
        /// </summary>
        /// <param name="context">The upgrade context to evaluate for applicability.</param>
        /// <param name="token">A cancellation token.</param>
        /// <returns>True if the upgrade step should be displayed to the user; false otherwise.</returns>
        protected override Task<bool> IsApplicableImplAsync(IUpgradeContext context, CancellationToken token)
        {
            // Because this upgrade step works at the project level, it is only applicable if there is a current project available.
            // If, for example, the user hasn't selected which project to upgrade yet, then this step will not apply.
            // Also, the upgrade step only applies if string replacements have been supplied.
            return Task.FromResult(context?.CurrentProject is not null && StringsToReplace.Any());
        }

        /// <summary>
        /// This method runs when the Upgrade Assistant is considering this upgrade step to run next. The method needs to
        /// determine whether the step should be run (or if, for example, it has no work to do) and return an
        /// appropriate UpgradeStepInitializeResult. This method can also prepare state that the upgrade step
        /// will need to execute. In the case of the FindReplaceStep, InitializeImplAsync will return an incomplete
        /// result if any source files in the project have strings that need replaced and false otherwise.
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

            // The IProject abstraction exposes a project.
            var currentProject = context.CurrentProject.Required();

            // Prepare state by identifying necessary replacements
            var compiledItems = currentProject.FindFiles(ProjectItemType.Compile, ".cs");
            var stringsToReplace = StringsToReplace.Keys;
            _neededReplacements = new Dictionary<string, IEnumerable<string>>();
            foreach (var itemPath in compiledItems)
            {
                var contents = File.ReadAllText(itemPath);
                var keysFound = stringsToReplace.Where(s => contents.Contains(s));
                if (keysFound.Any())
                {
                    Logger.LogDebug("Found {ReplacementCount} distinct strings needing replacement in {Path}", keysFound.Count(), itemPath);
                    _neededReplacements.Add(itemPath, keysFound);
                }
            }

            Logger.LogInformation("Found {FileCount} files needing string replacements", _neededReplacements.Count);

            // Return an appropriate UpgradeStepInitializeResult based on whether any replacements are needed
            return Task.FromResult(_neededReplacements.Any()
                ? new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, $"Replacements needed in {_neededReplacements.Count} files", BuildBreakRisk.Medium)
                : new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, "No string replacements needed", BuildBreakRisk.None));
        }

        /// <summary>
        /// The ApplyImplAsync step is invoked when the user attempts to actually apply the upgrade step and should make
        /// necessary changes to the upgrade context. In the case of the FindReplaceStep, ApplyImplAsync will replace strings
        /// based on the results of the initialize method.
        /// </summary>
        /// <param name="context">The upgrade context to update.</param>
        /// <param name="token">A cancellation token.</param>
        /// <returns>A result indicating whether the upgrade step applied successfully or not.</returns>
        protected override Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (_neededReplacements is null)
            {
                Logger.LogError("Could not apply FindReplaceStep because the step has not been properly initialized");
                return Task.FromResult(new UpgradeStepApplyResult(UpgradeStepStatus.Failed, "FindReplaceStep cannot be applied before it is initialized"));
            }

            // Apply the necessary changes for this upgrade step
            foreach (var path in _neededReplacements.Keys)
            {
                Logger.LogTrace("Replacing strings in {FilePath}", path);
                var replacements = _neededReplacements[path];
                var contents = File.ReadAllText(path);
                foreach (var key in replacements)
                {
                    contents = contents.Replace(key, StringsToReplace[key]);
                }

                File.WriteAllText(path, contents);
            }

            Logger.LogInformation("Strings replaced in {Count} files", _neededReplacements.Keys.Count);
            return Task.FromResult(new UpgradeStepApplyResult(UpgradeStepStatus.Complete, $"Strings replaced in {_neededReplacements.Keys.Count} files"));
        }

        // ***** ***** *****
        // Optional overrides
        // ***** ***** *****

        /// <summary>
        /// Gets a list of upgrade step IDs representing upgrade steps that this step must execute after.
        /// This sample step should execute after a project has been selected and after the TFM has been
        /// updated (since, presumably, the string replacements are meant to work with the updated TFM).
        /// </summary>
        public override IEnumerable<string> DependsOn { get; } = new[]
        {
            // The user must select a specific project before updating the authors property
            WellKnownStepIds.CurrentProjectSelectionStepId,
            WellKnownStepIds.SetTFMStepId
        };

        /// <summary>
        /// Gets a list of upgrade step IDs representing upgrade steps that this step must execute before.
        /// This sample step should execute before completing project upgrade and moving to the next project.
        /// </summary>
        public override IEnumerable<string> DependencyOf { get; } = new[]
        {
            WellKnownStepIds.NextProjectStepId
        };

        /// <summary>
        /// The Reset method is called in between uses of the Upgrade Step (when the current project changes, for example)
        /// and should undo initialization so that the step can be used again with a different context. In the case of FindReplaceStep,
        /// this includes resetting internal state.
        /// </summary>
        /// <returns>An UpgradeStepInitializeResult indicating the state of the upgrade step.</returns>
        public override UpgradeStepInitializeResult Reset()
        {
            _neededReplacements = null;
            return base.Reset();
        }

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
